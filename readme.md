#  SQL Server → Dataverse Sync using Azure Functions

##  Project Overview
This project implements an automated and scheduled data synchronization system that transfers changes from SQL Server to Microsoft Dataverse (Dynamics 365) using Azure Functions and Power Automate.

The system detects INSERT, UPDATE, and DELETE operations in SQL Server and reflects them in Dataverse without manual intervention.

---

##  Objective
✔ Monitor SQL Server changes  
✔ Trigger Azure Function on schedule  
✔ Send changed records as JSON  
✔ Automatically update Dataverse  
✔ Avoid duplicate sync  
✔ Enterprise-ready solution  

---

##  Architecture

SQL Server  
   ↓ (Change Tracking)  
Azure Function (Timer Trigger)  
   ↓ (HTTP POST JSON)  
Power Automate Flow  
   ↓  
Dataverse (D365)

---

##  Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | Azure Functions (.NET 8 C#) |
| Database | SQL Server / Azure SQL |
| Change Detection | SQL Change Tracking |
| Integration | Power Automate |
| Target | Microsoft Dataverse (D365) |
| IDE | Visual Studio 2022 |
| Version Control | Git + GitHub |

---

##  How It Works

### Step 1
Azure Function runs every 60 seconds (Timer Trigger)

### Step 2
SQL Change Tracking fetches only modified rows

### Step 3
Function converts rows → JSON

### Step 4
HTTP POST sent to Power Automate

### Step 5
Flow inserts/updates/deletes records in Dataverse

---


================= Author ==================

Ayazkhan Pathan

GitHub: https://github.com/AyazKhan20

LinkedIn: https://www.linkedin.com/in/ayazkhan-pathan-43302b357/
