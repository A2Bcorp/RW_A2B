#!/bin/bash

#   This shell script is used to encrypt an archive containing the 
#   RimWorld dll files using the included public key. The purpose of
#   this is to enable Travis CI builds, which require these DLLs to
#   successfully build the mod. The Travis CI server contains the 
#   corresponding private key and will decrypt the archive during
#   the build process. The decrypted DLLs will not be distributed,
#   stored, or otherwise handled beyond verifying the build's status.
#   Note that only the Linux version's DLLs can be used for this purpose.

set -e
set -u

function cleanup {
  rm -f ../dlls.7z
  rm -f key.bin
}

trap cleanup ERR

SSL_DIR="Source/A2B/Source-DLLs/ssl"
TRAVIS_URL="https://api.travis-ci.org/repos/TehJoE/RW_A2B/key"

pushd $SSL_DIR

PUBLIC_KEY="rw_key.pub.pem"

# Get the public key from travis
wget -O - $TRAVIS_URL \
  | grep -Po '(?<="key":")[^"]*' \
  | sed -e "s/\\\n/\n/g" > $PUBLIC_KEY

# Encrypt a 256-bit random key
openssl rand -base64 32 > key.bin
openssl rsautl -encrypt -inkey $PUBLIC_KEY -pubin -in key.bin -out key.bin.enc

# Create the archive
7z a ../dlls.7z ../*.dll

# Encrypt the archive and remove the original and the key
openssl enc -aes-256-cbc -salt -in ../dlls.7z -out ../dlls.7z.enc -pass file:./key.bin
rm -f ../dlls.7z
rm -f key.bin

popd

echo "Finished"
