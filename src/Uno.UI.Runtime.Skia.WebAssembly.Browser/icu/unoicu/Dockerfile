# Build this Dockerfile with  "docker build --output type=local,dest=. ." to get the unoicu.a file in the currect directory
FROM docker.io/emscripten/emsdk:4.0.15 AS build

RUN apt-get update && apt-get install -y make git python3 doxygen zip wget g++

WORKDIR /icu

RUN wget https://github.com/unicode-org/icu/archive/refs/tags/release-77-1.zip
RUN unzip release-77-1.zip
WORKDIR icu-release-77-1/icu4c/source

# Thanks to https://github.com/echogarden-project/icu-segmentation-wasm/blob/1f0c7381f76e0d38784dd6ad23fed5f6365da319/docs/Building.md
# for documenting the necessary configure options

RUN emconfigure ./configure --enable-static --disable-shared --with-data-packaging=static --disable-tests --disable-samples --disable-tools --disable-extras CFLAGS="-O3 -std=c11" CXXFLAGS="-O3 -std=c++17"
RUN emmake make -j $(nproc)

COPY unoicu.c .
RUN emcc -I common -c unoicu.c
RUN emar x lib/libicuuc.a
RUN emar x stubdata/libicudata.a
RUN emar rc unoicu.a *.ao unoicu.o


FROM scratch
COPY --from=build /icu/icu-release-77-1/icu4c/source/unoicu.a /
