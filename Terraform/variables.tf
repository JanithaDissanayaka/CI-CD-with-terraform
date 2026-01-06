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
    default = "192.168.8.140/32"
}
variable "jenkins_ip" {
    default = "111.223.177.181/32"
  
}
variable "instance_type" {
    default = "t3.small"
}