#!/usr/bin/env bash
set -euo pipefail

BINARY_NAME="cpool"
INSTALL_DIR="$HOME/.local/bin"
BINARY_PATH="$INSTALL_DIR/$BINARY_NAME"

if [[ -f "$BINARY_PATH" ]]; then
    rm "$BINARY_PATH"
    echo "Removed $BINARY_PATH"
else
    echo "Nothing to remove — $BINARY_PATH does not exist."
fi

for rc in "$HOME/.bashrc" "$HOME/.zshrc"; do
    if [[ -f "$rc" ]] && grep -qF "# added by cpool installer" "$rc"; then
        sed -i.bak '/# added by cpool installer/d' "$rc"
        rm -f "${rc}.bak"
        echo "Removed PATH entry from $(basename "$rc")"
    fi
done

echo "Done. Open a new terminal for the PATH change to take effect."
