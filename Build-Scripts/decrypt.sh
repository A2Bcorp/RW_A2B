#!/bin/bash

# Decryption script for Travis CI

set -e
set -u

DLL_DIR="Source/A2B/Source-DLLs"
PRIVATE_KEY="~/.ssh/id_rsa.pem"

pushd $DLL_DIR

# Convert the private key file to pem
openssl rsa -in ~/.ssh/id_rsa -outform pem > $PRIVATE_KEY

# Decrypt the encoded key file
openssl rsautl -decrypt -inkey $PRIVATE_KEY -in ssl/key.bin.enc -out ssl/key.bin

# Decrypt the archive
openssl enc -d -aes-256-cbc -in dlls.7z.enc -out dlls.7z -pass file:ssl/key.bin

# Extract the archive
7z x dlls.7z

popd

echo "Finished"

exit 0
