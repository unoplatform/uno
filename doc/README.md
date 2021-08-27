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

### Node

Use a node version manager or the version of node specified in the `.nvmrc` file nvm or nvs

```
nvs use
```
or
```
nvm use
```

Then install de dependencies
```
npm install
```

## To see your changes run
With browser sync and gulp watch, any changes in the scss, js and Docfx templates should be reserved automatically.
```
npm start
```

## and to build the documentation use

```
npm run generate
```

The local environement is usually located on port `3000` unless another process is already using it.

You have to remove the `docs` fragment from the wordpress menu to reach your local documentation server.
