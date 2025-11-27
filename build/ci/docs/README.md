# Documentation Deployment Configuration

This document describes the Azure DevOps pipeline configuration required for deploying the Uno Platform documentation to Canary, Staging, and Production environments.

## Overview

The documentation deployment pipeline is configured in `.azure-devops-stages-docs.yml` and consists of the following stages:

1. **Setup**: Validates commits, spelling, and markdown formatting
2. **Docs Generation**: Builds both canary and staging documentation
3. **Deploy Canary**: Deploys to canary environment (automatic on master)
4. **Deploy Staging**: Deploys to staging environment (automatic on master/stable)
5. **Deploy Production**: Deploys to production (manual approval required)

## Required Azure DevOps Variables

The following pipeline variables must be configured in Azure DevOps:

### Storage Account Names

- `DocsCanaryStorageAccount`: Azure Storage Account name for the canary environment
- `DocsStagingStorageAccount`: Azure Storage Account name for the staging environment
- `DocsProdStorageAccount`: Azure Storage Account name for the production environment

### Service Connection

- An Azure service connection named `Uno Platform Azure Subscription` must be configured with permissions to deploy to the storage accounts.

## Required Azure Environments

The following Azure DevOps environments must be created:

1. **Uno Platform Docs Canary**: No approvals required (automatic deployment)
2. **Uno Platform Docs Staging**: No approvals required (automatic deployment)
3. **Uno Platform Docs Production**: Manual approval required before deployment

## Deployment Triggers

### Canary Deployment
- **Trigger**: Automatic on every commit to `master` branch
- **Source**: Default/main branches of all repositories
- **Purpose**: Preview upcoming documentation changes

### Staging Deployment
- **Trigger**: Automatic on commits to `master` or `release/stable/*` branches
- **Source**: Stable release branches or default branches
- **Purpose**: Final verification before production

### Production Deployment
- **Trigger**: Manual approval after successful staging deployment
- **Source**: Same as staging (stable release branches)
- **Purpose**: Live documentation for public consumption

## Build Artifacts

The pipeline produces two separate documentation artifacts:

1. **DocumentationArtifacts-Canary**: Built from default branches
2. **DocumentationArtifacts-Staging**: Built from stable branches

## Import Scripts

Three PowerShell scripts manage external documentation imports:

1. `import_external_docs.ps1`: Imports from stable release branches (for staging/production)
2. `import_external_docs_canary.ps1`: Imports from default branches (for canary)
3. `import_external_docs_test.ps1`: Local testing script with optional fetch

## MSBuild Targets

Two MSBuild targets generate documentation:

1. `GenerateDoc`: Generates staging/production documentation
2. `GenerateDocCanary`: Generates canary documentation

## Azure Storage Configuration

Each storage account must be configured for static website hosting:

1. Enable static website hosting in the storage account
2. Set the default document to `articles/intro.html` or appropriate entry point
3. Configure the `$web` container for public blob access

## Troubleshooting

### Documentation not deploying

Check that:
- The pipeline variables are correctly set
- The Azure service connection has appropriate permissions
- The Azure environments are created and configured
- Storage accounts have static website hosting enabled

### Canary not updating

Verify:
- The commit was pushed to the `master` branch
- The `deploy_docs_canary` stage condition is met
- The `import_external_docs_canary.ps1` script is pulling latest commits

### Production deployment not available

Ensure:
- Staging deployment completed successfully
- The manual approval is configured in the Azure environment
- The branch is `master` or starts with `release/stable/`
