#/usr/bin/env bash
set -euo pipefail

pushd . > /dev/null

mkdir -p angle
cd angle

# It's not enough to check for the existence of depot_tools/.git as the cloning may have been interrupted.
# The git status call makes sure that the repo is not corrupted.
if [ ! -d  depot_tools/.git ] || ! git -C depot_tools status; then
    rm -rf depot_tools
    git clone https://chromium.googlesource.com/chromium/tools/depot_tools.git
fi

export PATH="$(pwd)/depot_tools:${PATH}"

if [ ! -f angle_source/angle_fetched_successfully ]; then
    rm -rf angle_source
    mkdir angle_source
    cd angle_source
    fetch angle
    touch angle_fetched_successfully
else
    cd angle_source
    echo "Already fetched angle sources. Skipping fetch."
fi

for arch in arm64 x64
do
    if [ ! -f "out/Release_${arch}/build_succeeded" ]; then    
        rm -rf "out/Release_${arch}"
        mkdir -p "out/Release_${arch}"
        
        cat << EOF > "out/Release_${arch}/args.gn"
target_cpu = "${arch}"
is_debug = false
angle_assert_always_on = true
EOF

        gn gen "out/Release_${arch}"
        autoninja -C "out/Release_${arch}"
        touch "out/Release_${arch}/build_succeeded"
    else
        echo "Already compiled angle for ${arch}. Skipping ${arch} compilation."
    fi
done

popd > /dev/null

mkdir -p angle_binaries/osx

# create universal mac binaries
for lib in libEGL.dylib libGLESv2.dylib
do
    lipo -create angle/angle_source/out/Release_arm64/${lib} angle/angle_source/out/Release_x64/${lib} -output angle_binaries/osx/${lib}
done