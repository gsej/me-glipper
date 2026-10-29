provider "proxmox" {
  endpoint = var.proxmox_endpoint
  insecure = var.proxmox_tls_insecure

  # Authenticate as root@pam (username/password). Docker-in-LXC needs the
  # keyctl feature flag, and Proxmox restricts setting feature flags (other
  # than nesting) to root@pam — an API token cannot do it, so token auth is
  # not used here.
  username = var.proxmox_username
  password = var.proxmox_password
}
