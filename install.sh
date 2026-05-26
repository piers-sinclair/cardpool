#!/usr/bin/env bash
set -euo pipefail

BINARY_NAME="cpool"
INSTALL_DIR="$HOME/.local/bin"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BINARY_SRC="$SCRIPT_DIR/$BINARY_NAME"

if [[ ! -f "$BINARY_SRC" ]]; then
    echo "Error: $BINARY_NAME not found next to this script. Make sure both files are in the same folder." >&2
    exit 1
fi

echo "Installing $BINARY_NAME to $INSTALL_DIR ..."
mkdir -p "$INSTALL_DIR"
cp "$BINARY_SRC" "$INSTALL_DIR/$BINARY_NAME"
chmod +x "$INSTALL_DIR/$BINARY_NAME"

PATH_LINE='export PATH="$HOME/.local/bin:$PATH"  # added by cpool installer'
path_added=false

for rc in "$HOME/.bashrc" "$HOME/.zshrc"; do
    if [[ -f "$rc" ]] && ! grep -qF "# added by cpool installer" "$rc"; then
        printf '\n%s\n' "$PATH_LINE" >> "$rc"
        echo "Added $INSTALL_DIR to PATH in $(basename "$rc")"
        path_added=true
    fi
done

if [[ "$path_added" == false ]]; then
    echo "$INSTALL_DIR is already in your PATH configuration."
fi

echo ""
echo "Done! Open a new terminal and run: cpool --help"
