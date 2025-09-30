# Build this Dockerfile with  "docker build --output type=local,dest=. ." to get the .dat file in the currect directory
FROM docker.io/ubuntu:24.04 AS build


RUN apt-get update && apt-get install -y make git python3 doxygen zip wget g++

WORKDIR /icu

ENV ICU_DATA_FILTER_FILE=/icu/filters.json
COPY filters.json .

RUN wget https://github.com/unicode-org/icu/archive/refs/tags/release-77-1.zip
RUN unzip release-77-1.zip
WORKDIR icu-release-77-1/icu4c/source

RUN ./runConfigureICU Linux
RUN make -j $(nproc)


FROM scratch
COPY --from=build /icu/icu-release-77-1/icu4c/source/data/out/tmp/icudt77l.dat /icudt.dat
