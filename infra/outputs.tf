output "container_id" {
  description = "VMID of the me-glipper container"
  value       = proxmox_virtual_environment_container.meglipper.vm_id
}

output "container_ip" {
  description = "IPv4 address of the container"
  value       = local.container_ip
}

output "api_url" {
  description = "me-glipper API on the LAN (phase 1)"
  value       = "http://${local.container_ip}:5200"
}
