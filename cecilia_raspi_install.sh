#!/usr/bin/env bash

# cecilia_raspi_install.sh
# Version: 1.0
# Last Changed 16/08/2020:
    # 16/08/2020 - Initial script

# This is an automated script to install the latest image of Cecilia from the Docker Hub Page
# It is designed and tested to run on a stock install of Raspberry Pi OS (32-bit) Lite based on Debian Buster
# Please only execute on a fresh install

function out80Line {
    echo "================================================================================"
}

# Output nice starting messages
echo "This is the automated install script for Cecilia, the Discord Music Bot"
echo "It is designed to be run on a fresh install of Raspberry Pi OS (32-bit) Lite"
echo "It will run mostly unattended, but there are couple of points you need to input key presses"
echo "So please monitor the script as it runs!"
read -p "Press enter to continue, or CTRL + C to exit . . . "

# Check if user has docker group
echo "First we will need to add you to the Docker group"
echo "This will close the script. Immediately rexecute the script when you can."
read -p "Press enter to continue . . . "

if [ $(getent group docker) ]; then
    echo "Already in docker group. Setup will continue"
else
    sudo groupadd docker
    sudo usermod -aG docker $USER
    newgrp docker
fi

# Check we have 4 parameters for all tokens
if [ $# != 4 ]; then
    echo "This script requires 4 parameters in this order:"
    echo "1. Discord Bot Token"
    echo "2. Discord Command Prefix"
    echo "3. Spotify ClientID (Enter -1 to disable)"
    echo "4. Spotify ClientSecret (Enter -1 to disable)"
    echo "Please re-run the script with these parameters"
    exit
fi

# Store variables
BOT_TOKEN=$1 COMMAND_PREFIX=$2 SPOTIFY_ID=$3 SPOTIFY_CLIENT=$4

# 0. Install jq
sudo apt update
sudo apt install jq -y

# 1. Create raw JSON strings and echo back waiting for user to confirm
out80Line
BOT_JSON_STRING=$( jq -n \
                  --arg tk "$BOT_TOKEN" \
                  --arg pre "$COMMAND_PREFIX" \
                  '{Token: $tk, Prefix: $pre}' )
echo "Your bot config looks like: $BOT_JSON_STRING"
SPOTIFY_JSON_STRING=$( jq -n \
                  --arg id "$SPOTIFY_ID" \
                  --arg sec "$SPOTIFY_CLIENT" \
                  '{ClientId: $id, ClientSecret: $sec}' )
echo "Your spotify config looks like: $SPOTIFY_JSON_STRING"
out80Line
echo "If these token values do not look right please exit with CTRL+C and rerun the script with correct values."
read -p "Press enter to continue, or CTRL + C to exit"

# 2.First install dockser and required packages
out80Line
echo "Lets install Docker and some required packages"
out80Line

sudo apt install -y \
     apt-transport-https \
     ca-certificates \
     curl \
     gnupg2 \
     software-properties-common 

# Get the Docker signing key for packages
curl -fsSL https://download.docker.com/linux/$(. /etc/os-release; echo "$ID")/gpg | sudo apt-key add -

# Add the Docker official repos
echo "deb [arch=$(dpkg --print-architecture)] https://download.docker.com/linux/$(. /etc/os-release; echo "$ID") \
     $(lsb_release -cs) stable" | \
    sudo tee /etc/apt/sources.list.d/docker.list

# Install Docker
sudo apt update
sudo apt install -y --no-install-recommends \
    docker-ce \
    cgroupfs-mount

# Enable docker on startup and start now
sudo systemctl enable docker
sudo systemctl start docker

# 4. Now this script basically follows the docker hub instructions
out80Line
echo "Time to create your Cecilia bot!"
out80Line
# Pull the image
docker pull mgrime/cecilia:arm
# Make the config direction
mkdir $HOME/cecilia_config
# Create Docker container
docker create --name cecilia -v $HOME/cecilia_config:/App/Config mgrime/cecilia:arm
# Start and stop container
docker start cecilia
docker stop cecilia
# Output into .json files
echo $BOT_JSON_STRING >> $HOME/cecilia_config/bot.json
echo $SPOTIFY_JSON_STRING >> $HOME/cecilia_config/spotify.json
# Set to run on startup
docker update --restart=always cecilia
# Start the container
docker start cecilia
# Show running
docker ps -a
out80Line
echo "Script finished! Cecilia should now be running, if you provided valid token. NOTE: If you reboot the Pi you will need to start the container with command:"
echo "  docker start cecilia"
out80Line
exit
    