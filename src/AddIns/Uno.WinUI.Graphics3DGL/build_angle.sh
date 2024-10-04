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
    fetch --no-history angle

    # remove unnecessary dirs for space
    rm -rf third_party/dawn third_party/VK-GL-CTS
    find ./tools ./third_party -name "*.git" | xargs rm -rf

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
        
        # we remove as much as we can from the build to save space for the space-limited CI machines
        cat << EOF > "out/Release_${arch}/args.gn"
target_cpu = "${arch}"
is_debug = false
angle_assert_always_on = true
angle_build_tests = false
build_angle_deqp_tests = false
build_angle_perftests = false
angle_enable_vulkan = false
angle_enable_wgpu = false
angle_enable_abseil = false
angle_enable_gl_null = false
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
shopt -s nullglob
for lib in libEGL.* libGLESv2.*
do
    lipo -create angle/angle_source/out/Release_arm64/${lib} angle/angle_source/out/Release_x64/${lib} -output angle_binaries/osx/${lib}
done
shopt -u nullglob