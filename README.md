REST API service for searching available medical appointment slots.
    
    To run this solution you will need:
    1. Installed .net10 SDK
    2. Call "dotent run" in Scheduler.Api project folder
 

---

## 🛠 Tech Stack

- .NET 10
- Entity Framework Core
- SQL Database (SQLite)
- Swagger / OpenAPI
- Unit Tests (xUnit, FluentAssertions)

---

## 📌 Project Description

The application allows searching for available appointment slots in a medical facility.

The system takes into account:

- Doctor schedules (single and recurring)
- Existing booked appointments
- Slot duration
- Only future time slots

---

## 📡 API Endpoint

### `GET /api/availability`

### Query Parameters

| Parameter | Required | Description |
|------------|----------|-------------|
| `specializationId` | ✅ | Specialization identifier |
| `doctorId` | ❌ | Doctor identifier |
| `from` | ❌ | Search start date |
| `to` | ❌ | Search end date |
| `slotDurationMinutes` | ✅ | Length of appointment slot (minutes) |
| `maxResults` | ✅ | Maximum number of returned results |

### Validation Rules

- Cannot search by doctor without specialization
- `slotDurationMinutes` must be within safe limits (max 480 minutes)
- `maxResults` is limited to prevent heavy queries (max 1000)
- If `from` is not provided → current time is used
- Search window – limited to a maximum of 3 months.
- If no schedules exist → empty list is returned

---

## 📤 Example Response

```json
[
  {
    "doctorName": "Karol Sercowy",
    "specialization": "Kardiologia",
    "startTime": "2025-09-16T09:00:00",
    "endTime": "2025-09-16T16:00:00"
  }
]
