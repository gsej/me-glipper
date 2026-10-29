locals {
  # Bare IP (without the CIDR suffix) for outputs and the Ansible inventory.
  container_ip = split("/", var.container_ip_cidr)[0]
}

resource "proxmox_download_file" "os_template" {
  content_type = "vztmpl"
  datastore_id = var.template_datastore_id
  node_name    = var.proxmox_node
  url          = var.template_url

  # Keeps this repo standalone: another service's Terraform may have already
  # fetched the same template to this datastore, so adopt that existing file
  # instead of erroring rather than depending on another repo running first.
  overwrite_unmanaged = true
}

resource "proxmox_virtual_environment_container" "meglipper" {
  node_name = var.proxmox_node
  vm_id     = var.container_id

  # Unprivileged: this is an internet-facing service (Tailscale Funnel, set up
  # by Ansible in userspace-networking mode so no /dev/net/tun is needed) and it
  # needs no device passthrough, so the stronger isolation of an unprivileged
  # container is the right call. nesting + keyctl are still needed for
  # Docker-in-LXC (both require root@pam auth — see providers.tf).
  unprivileged = true

  features {
    nesting = true
    keyctl  = true
  }

  operating_system {
    template_file_id = proxmox_download_file.os_template.id
    type             = "ubuntu"
  }

  cpu {
    cores = var.cpu_cores
  }

  memory {
    dedicated = var.memory_mb
    swap      = 512
  }

  disk {
    datastore_id = var.datastore_id
    size         = var.rootfs_size_gb
  }

  # Dedicated volume for the SQLite DB so a container rebuild never eats the
  # data. Mounted at the DB dir; .env sits in the parent dir on the rootfs.
  mount_point {
    volume = var.storage_datastore_id
    size   = "${var.data_size_gb}G"
    path   = "/opt/me-glipper-data/database"
  }

  network_interface {
    name   = "eth0"
    bridge = var.network_bridge
  }

  initialization {
    hostname = var.hostname

    ip_config {
      ipv4 {
        address = var.container_ip_cidr
        gateway = var.container_gateway
      }
    }

    dns {
      servers = var.dns_servers
    }

    user_account {
      keys     = [trimspace(file(pathexpand(var.ssh_public_key_path)))]
      password = var.container_root_password
    }
  }

  startup {
    order = 1
  }

  start_on_boot = true
  started       = true

  tags = ["me-glipper", "terraform"]
}
