#!/bin/bash

# Decryption script for Travis CI

set -e
set -u

DLL_DIR="Source/A2B/Source-DLLs"

pushd $DLL_DIR

openssl aes-256-cbc -K $encrypted_d8f806c9345c_key -iv $encrypted_d8f806c9345c_iv -in dlls.7z.enc -out dlls.7z -d

# Extract the archive
7z x dlls.7z

popd

echo "Finished"

exit 0
