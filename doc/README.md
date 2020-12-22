# Documentation Uno

This folder contains source code for the generation of uno's documentation

# Running a local environement

## Install dependencies

Download and install docfx on your computer.

### MacOS

```
brew install docfx
```

### Windows

```
choco install docfx
```

```
npm install
```

## To see your changes run
```
gulp
```

## and to build the documentation use

```
npm run generate
```

The local environement is usually located on port `3000` unless another process is already using it.

You have to remove the `docs` fragment from the wordpress menu to reach your local documentation server.
