# ImageService API

A simple ASP.NET Core Web API for uploading, retrieving, resizing, and deleting images. Images are stored in Azure Blob Storage, and resized variations (including thumbnails) are generated on demand.

---

## Features

- Upload an image via `multipart/form-data`
- Retrieve the original image path
- Get resized versions by specifying height
- Automatically generate a 160px height thumbnail
- Delete all stored versions of an image
- Swagger UI for testing endpoints

---

## Requirements

- [.NET 6 SDK or later](https://dotnet.microsoft.com/en-us/download)
- Visual Studio, VS Code, or any IDE that supports C#

---

## Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/Axeleron007/ImageServiceTest
cd ImageService
```

## Running the API Locally

1. Open the solution in Visual Studio.

2. Right-click ImageService.API and select Set as Startup Project.

3. Press F5 to launch with debugging or Ctrl + F5 to run without debugging.

## API Documentation (Swagger UI)

Swagger UI is enabled by default in the development environment.

Once the service is running, navigate to:
https://localhost:5001/swagger
This will open the interactive API documentation.

## Main API Endpoints

Method	Endpoint					Description
POST	/api/images/upload			Upload a new image (multipart/form-data)
GET		/api/images/{id}			Retrieve the original image path
GET		/api/images/{id}/variation	Get a resized image by height (targetHeight)
GET		/api/images/{id}/thumbnail	Get a 160px thumbnail of the image
DELETE	/api/images/{id}			Delete all versions of the specified image