#!/usr/bin/env bash
set -euo pipefail

fix_yarn_gpg_key() {
  local yarn_list_file="/etc/apt/sources.list.d/yarn.list"
  local keyring_dir="/etc/apt/keyrings"
  local keyring_file="${keyring_dir}/yarn-archive-keyring.gpg"

  if [[ ! -f "${yarn_list_file}" ]]; then
    return 0
  fi

  echo "Detected Yarn apt source. Repairing Yarn repository key..."
  sudo mkdir -p "${keyring_dir}"
  curl --proto '=https' -fsSL https://dl.yarnpkg.com/debian/pubkey.gpg | gpg --dearmor | sudo tee "${keyring_file}" >/dev/null
  echo "deb [signed-by=${keyring_file}] https://dl.yarnpkg.com/debian/ stable main" | sudo tee "${yarn_list_file}" >/dev/null
}

run_apt_update() {
  if sudo apt-get update; then
    return 0
  fi

  fix_yarn_gpg_key
  sudo apt-get update
}

if command -v az >/dev/null 2>&1; then
  echo "Azure CLI is already installed: $(az version --query '"azure-cli"' -o tsv 2>/dev/null || az version | head -n 1)"
  exit 0
fi

echo "Installing Azure CLI..."
run_apt_update
sudo apt-get install -y ca-certificates curl apt-transport-https lsb-release gnupg

sudo mkdir -p /etc/apt/keyrings
curl --proto '=https' -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor | sudo tee /etc/apt/keyrings/microsoft.gpg >/dev/null

AZ_REPO="$(lsb_release -cs)"
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/microsoft.gpg] https://packages.microsoft.com/repos/azure-cli/ ${AZ_REPO} main" | sudo tee /etc/apt/sources.list.d/azure-cli.list >/dev/null

run_apt_update
sudo apt-get install -y azure-cli

echo "Installed: az $(az version --query '"azure-cli"' -o tsv 2>/dev/null || true)"
