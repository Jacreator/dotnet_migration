-- Local only: create the database the legacy app uses.
-- The app signs in with your Windows account (Integrated Security), so no SQL login or password is needed.
--   sqlcmd -S localhost\SQLEXPRESS -E -i setup-local.sql
-- EF6 creates the Readings table and seed data on the first request.

IF DB_ID('ReadingsDb') IS NULL CREATE DATABASE ReadingsDb;
GO
