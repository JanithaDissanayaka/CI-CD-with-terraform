provider "aws" {
    region = "ap-south-1"
}

resource "aws_vpc" "myapp-vpc" {
    cidr_block = var.vpc_cidr_blocks

    tags = {
      Name: "${var.env_prefix}-vpc"
    }
  
}

resource "aws_subnet" "myapp-subnet-1" {
    vpc_id = aws_vpc.myapp-vpc.id
    cidr_block = var.subnet_cidr_blocks


    tags = {
      Name: "${var.env_prefix}-subnet-1"
    }
  
}

resource "aws_internet_gateway" "myapp-igw" {
  vpc_id = aws_vpc.myapp-vpc.id
  tags = {
    Name="${var.env_prefix}-igw"
  }
  
}


resource "aws_default_route_table" "default-rtb" {
  default_route_table_id = aws_vpc.myapp-vpc.default_route_table_id

  route{
    cidr_block="0.0.0.0/0"
    gateway_id=aws_internet_gateway.myapp-igw.id
  }

  tags = {
    Name="${var.env_prefix}-main-rtb"
  }
  
}

resource "aws_default_security_group" "default-sg" {
  vpc_id = aws_vpc.myapp-vpc.id

  ingress{
    from_port=22
    to_port=22
    protocol="tcp"
    cidr_blocks = [var.my_ip,var.jenkins_ip]
  }
  
  ingress{
    from_port=80
    to_port=80
    protocol="tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress{
    from_port=0
    to_port=0
    protocol="-1"
    cidr_blocks = ["0.0.0.0/0"]
    prefix_list_ids = []
  }

  tags = {
     Name="${var.env_prefix}-default-sg"
  }
  
}

data "aws_ami" "image" {
  most_recent = true
  owners      = ["amazon"]

  filter {
    name   = "name"
    values = ["al2023-ami-2023.*-kernel-6.1-x86_64"]
  }

  filter {
    name   = "architecture"
    values = ["x86_64"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }

  filter {
    name   = "root-device-type"
    values = ["ebs"]
  }
}

resource "aws_instance" "myapp-image" {
  ami= data.aws_ami.image.id
  instance_type = var.instance_type

  subnet_id =aws_subnet.myapp-subnet-1.id
  vpc_security_group_ids =[aws_default_security_group.default-sg.id]
  associate_public_ip_address = true
  key_name = "auction-site-key-pair"
  user_data = file("entryscript.sh")

  tags={
    name="${var.env_prefix}-ec2"
  }
}

output "ec2_public_ip" {
  value = aws_instance.myapp-image.public_ip
}