# Stage 6: pipeline (GitHub Actions with OpenID Connect)

The workflow is `.github/workflows/deploy.yml`: test, publish, deploy to the staging slot, check `/health`, swap.
It signs in to Azure with OpenID Connect, so GitHub stores no password or secret, only three IDs that are not secret.

Repository: `Jacreator/dotnet_migration`, branch `master`.

## Why a user-assigned managed identity, not an app registration

The usual guide creates an Entra app registration. The university directory does not let students register applications
("Directory permission is needed ... Insufficient privileges"). A user-assigned managed identity is an ordinary Azure
resource in the resource group, needs no directory permission, and can trust GitHub's OIDC tokens in exactly the same way.

## Setup (already done)

```bash
source infra/env.sh
REPO="Jacreator/dotnet_migration"

az provider register -n Microsoft.ManagedIdentity --wait
az identity create -g $RG -n id-github-deploy -l $LOC
```
Why: the identity the pipeline signs in as.

```bash
az identity federated-credential create -g $RG --identity-name id-github-deploy -n github-master \
  --issuer https://token.actions.githubusercontent.com \
  --subject "repo:$REPO:ref:refs/heads/master" \
  --audiences api://AzureADTokenExchange
```
Why: Azure accepts GitHub tokens for runs on `master` of this repo only. Nothing to rotate or leak.

```bash
az role assignment create --assignee-object-id $(az identity show -g $RG -n id-github-deploy --query principalId -o tsv) \
  --assignee-principal-type ServicePrincipal --role "Website Contributor" \
  --scope $(az webapp show -g $RG -n $WEB_APP --query id -o tsv)
```
Why: the pipeline can deploy to and swap this one web app, nothing else.

## GitHub variables (you do this in the browser)

Repository > **Settings** > **Secrets and variables** > **Actions** > **Variables** tab > **New repository variable**:

| Name | Value |
|---|---|
| `AZURE_CLIENT_ID` | `az identity show -g $RG -n id-github-deploy --query clientId -o tsv` |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |

Variables, not secrets: none of these IDs grants access on its own. Access comes from the federated trust above.

Then push to `master`, or run the workflow by hand from the Actions tab (choose the `master` branch).

## If it fails

- **"Not all values are present. Ensure 'client-id' and 'tenant-id' are supplied"**: the variables are missing or were added under **Secrets** instead of **Variables**.
- **"AADSTS70021: No matching federated identity record found"**: the run was not on `master` (for example a pull request or another branch). The subject must match exactly.
- **Authorization failed on deploy or swap**: the role assignment can take a few minutes to apply. Rerun the job.
