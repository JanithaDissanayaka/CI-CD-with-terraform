variable vpc_cidr_blocks {
    default = "10.0.0.0/16"
}
variable subnet_cidr_blocks {
    default = "10.0.1.0/24"
}
variable env_prefix {
    default = "dev"
}
variable my_ip {
    default = "ip_address"
}
variable "jenkins_ip" {
    default = "jenkins_ip"
  
}
variable "instance_type" {
    default = "t3.small"
}
