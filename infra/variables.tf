# ---- Proxmox connection ----------------------------------------------------

variable "proxmox_endpoint" {
  description = "Proxmox API endpoint, e.g. https://192.168.1.10:8006/"
  type        = string
}

variable "proxmox_tls_insecure" {
  description = "Skip TLS verification (true for a self-signed Proxmox cert)"
  type        = bool
  default     = true
}

variable "proxmox_username" {
  description = "Real Proxmox user for operations tokens can't do (setting keyctl etc.). Must be root@pam."
  type        = string
  default     = "root@pam"
}

variable "proxmox_password" {
  description = "Password for proxmox_username (the Proxmox host's root@pam password, NOT container_root_password)."
  type        = string
  sensitive   = true
}

variable "proxmox_node" {
  description = "Name of the Proxmox node to create the container on"
  type        = string
}

# ---- Container template ----------------------------------------------------

variable "template_url" {
  description = "URL of the LXC template to download onto the node"
  type        = string
  default     = "http://download.proxmox.com/images/system/ubuntu-24.04-standard_24.04-2_amd64.tar.zst"
}

variable "template_datastore_id" {
  description = "Datastore to store the downloaded template on"
  type        = string
  default     = "local"
}

# ---- Container -------------------------------------------------------------

variable "container_id" {
  description = "VMID for the container (null lets Proxmox pick one)"
  type        = number
  default     = null
}

variable "hostname" {
  description = "Container hostname"
  type        = string
  default     = "me-glipper"
}

variable "cpu_cores" {
  description = "vCPU cores (1 is fine; the build may be slow but runtime load is light)"
  type        = number
  default     = 1
}

variable "memory_mb" {
  description = "RAM in MiB. Start at 1 GB; bump if the .NET build OOMs."
  type        = number
  default     = 1024
}

variable "datastore_id" {
  description = "Datastore for the container root filesystem"
  type        = string
  default     = "local-lvm"
}

variable "rootfs_size_gb" {
  description = "Root filesystem size in GiB (holds the .NET SDK image + build layers)"
  type        = number
  default     = 16
}

variable "storage_datastore_id" {
  description = "Datastore for the data (DB) mount point"
  type        = string
  default     = "local-lvm"
}

variable "data_size_gb" {
  description = "Size of the data mount point in GiB (the SQLite DB is tiny; thin-provisioned)"
  type        = number
  default     = 1
}

# ---- Networking ------------------------------------------------------------

variable "container_ip_cidr" {
  description = "Static IPv4 address in CIDR form, e.g. 192.168.1.60/24"
  type        = string
}

variable "container_gateway" {
  description = "IPv4 gateway for the container"
  type        = string
}

variable "dns_servers" {
  description = "DNS servers for the container"
  type        = list(string)
  default     = ["192.168.1.1"]
}

variable "network_bridge" {
  description = "Proxmox bridge the container's NIC attaches to"
  type        = string
  default     = "vmbr0"
}

# ---- Access ----------------------------------------------------------------

variable "ssh_public_key_path" {
  description = "Public key injected into the container's root account (Ansible connects with the matching private key). Relative paths resolve from the infra/ working dir."
  type        = string
  default     = "../ansible/secrets/deploy_key.pub"
}

variable "container_root_password" {
  description = "Root password for the LXC container's own root account (console fallback)"
  type        = string
  sensitive   = true
}
