# Stage 6: pipeline (GitHub Actions with OpenID Connect)

The workflow is `.github/workflows/deploy.yml`: test, publish, deploy to the staging slot, check `/health`, swap.
It signs in to Azure with OpenID Connect, so GitHub stores no password or secret, only three IDs that are not secret.

Not run yet: it needs a GitHub repository. Replace `OWNER/REPO` below with yours.

```bash
source infra/env.sh
REPO="OWNER/REPO"

APP_ID=$(az ad app create --display-name "gh-readings-deploy" --query appId -o tsv)
az ad sp create --id $APP_ID -o none
```
Why: an Entra identity for the pipeline.

```bash
az ad app federated-credential create --id $APP_ID --parameters "{
  \"name\": \"github-main\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$REPO:ref:refs/heads/main\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}"
```
Why: Azure trusts tokens that GitHub issues for pushes to `main` of this repo only. Nothing to rotate or leak.

```bash
az role assignment create --assignee $APP_ID --role "Website Contributor" \
  --scope $(az webapp show -g $RG -n $WEB_APP --query id -o tsv)
```
Why: the pipeline can deploy to and swap this one web app, nothing else.

```bash
gh variable set AZURE_CLIENT_ID --body $APP_ID --repo $REPO
gh variable set AZURE_TENANT_ID --body $(az account show --query tenantId -o tsv) --repo $REPO
gh variable set AZURE_SUBSCRIPTION_ID --body $(az account show --query id -o tsv) --repo $REPO
```
Why: the three IDs the workflow reads. They are variables, not secrets, because none of them grants access on its own.

Then push to `main` (or run the workflow by hand from the Actions tab).
