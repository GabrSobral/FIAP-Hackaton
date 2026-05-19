-- Creates the dedicated database for the analysis-worker and report-service.
-- The api service uses the primary database (fiap_hackaton) created by POSTGRES_DB env var.
CREATE DATABASE worker_db;
