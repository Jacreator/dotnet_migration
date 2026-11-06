-- Database user for an app's managed identity: no password, it signs in with an Entra token.
-- Read and write data only. Variables: UserName (display name) and Sid (the identity's appId as 0x... bytes).
-- Using WITH SID means SQL does not need permission to look the identity up in Entra.

IF USER_ID('$(UserName)') IS NULL
    CREATE USER [$(UserName)] WITH SID = $(Sid), TYPE = E;

ALTER ROLE db_datareader ADD MEMBER [$(UserName)];
ALTER ROLE db_datawriter ADD MEMBER [$(UserName)];
