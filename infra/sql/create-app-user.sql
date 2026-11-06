-- Least-privilege database user for the old app, so it no longer uses the server admin login.
-- Contained user: it exists only inside ReadingsDb and can read and write data, nothing else.
-- Run with go-sqlcmd and pass the password as a variable (see infra/03-secrets-and-identity.md).

IF USER_ID('readings_app') IS NULL
    CREATE USER readings_app WITH PASSWORD = '$(AppPassword)';
ELSE
    ALTER USER readings_app WITH PASSWORD = '$(AppPassword)';

ALTER ROLE db_datareader ADD MEMBER readings_app;
ALTER ROLE db_datawriter ADD MEMBER readings_app;
