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
  curl -fsSL https://dl.yarnpkg.com/debian/pubkey.gpg | gpg --dearmor | sudo tee "${keyring_file}" >/dev/null
  echo "deb [signed-by=${keyring_file}] https://dl.yarnpkg.com/debian/ stable main" | sudo tee "${yarn_list_file}" >/dev/null
}

run_apt_update() {
  if sudo apt-get update; then
    return 0
  fi

  fix_yarn_gpg_key
  sudo apt-get update
}

if command -v pwsh >/dev/null 2>&1; then
  echo "PowerShell is already installed: $(pwsh --version)"
  exit 0
fi

echo "Installing PowerShell..."
run_apt_update
sudo apt-get install -y powershell

echo "Installed: $(pwsh --version)"
