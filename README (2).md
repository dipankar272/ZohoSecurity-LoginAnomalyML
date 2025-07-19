
# Anomaly Detection App - Step-by-Step Setup & Usage Guide

**Contact for Support:** dipankarthirupathi@gmail.com

---

## ✅ Step 1: Install Required Software

You must install these tools first, otherwise the project won't work:

1. **.NET 8.0 SDK**
   - Go to this link: [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Download and install .NET 8.0 SDK
   - After installing, open terminal or command prompt
   - Type: `dotnet --version`
   - If it shows something like `8.0.x`, installation is successful

2. **Docker**
   - For Windows or Mac: [https://www.docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)
   - For Linux: Run these commands in terminal:
   ```bash
   sudo apt-get update && sudo apt-get install docker-ce docker-ce-cli containerd.io
   sudo usermod -aG docker $USER
   ```
   - After that, restart your system to apply changes

---

## ✅ Step 2: Setup the Project

1. **Extract Project Files**
   - Create a folder where you want the project to be, for example:
     - On Windows: `C:\AnomalyDetectionApp`
     - On Linux/Mac: `~/AnomalyDetectionApp`
   - Extract all project files into that folder

2. **Create Required Folders**
   ```bash
   cd AnomalyDetectionApp
   mkdir Models data data/input data/training Config
   ```

3. **Add Configuration File**
   - Inside `Config` folder, create a file called `AppSettings.json`
   - Paste this content inside it:
   ```json
   {
     "ModelSaveDirectory": "Models/",
     "ModelFilePrefix": "savedmodel",
     "AnomalyThresholdSettings": {
       "IQRMultiplier": 1.5,
       "MinThreshold": 0.8
     }
   }
   ```

4. **Add Sample Data**
   - Put your training data CSV at: `data/training/corrected.csv`
   - Put your prediction data CSV at: `data/input/corrected.csv`

   CSV should look like this:
   ```csv
   User,Computer,Time,Date
   john,workstation1,09:30,2023-01-01
   sarah,laptop22,14:15,2023-01-01
   ```

---

## ✅ Step 3: Run the App without Docker

### For Training the Model:
```bash
dotnet run train data/training/corrected.csv
```

You should see output like:
```bash
--- STARTING MODEL TRAINING ---
ℹ️  [INFO] Fitting the anomaly detection model...
✅ [SUCCESS] Model training completed. Saved as version 1
ℹ️  [INFO] Model saved: Models/savedmodel1.zip
```

### For Predictions:
```bash
dotnet run predict data/input/corrected.csv 1
```

You should see output with detected anomalies

---

## ✅ Step 4: Run the App with Docker

### 1. Build Docker Image
```bash
docker build -t anomaly-detector .
```
Check it's built with:
```bash
docker images
```

### 2. Training with Docker
```bash
docker run --rm   -v "$(pwd)/Models:/data/models"   -v "$(pwd)/data:/app/data"   anomaly-detector train /app/data/training/corrected.csv
```

### 3. Prediction with Docker
```bash
docker run --rm   -v "$(pwd)/Models:/data/models"   -v "$(pwd)/data:/app/data"   anomaly-detector predict /app/data/input/corrected.csv 1
```

---

## ✅ Step 5: (Optional) Run with Batch Files (Windows Only)

If you're on Windows, you can double-click files to make it easy:

- `RunTraining.bat` → This runs training with Docker
- `RunPrediction.bat` → This runs predictions with Docker

---

## ✅ Common Problems & Solutions

| Problem               | Solution                                                                 |
|-----------------------|--------------------------------------------------------------------------|
| File not found        | Make sure your CSV files are placed in `data/training/corrected.csv` and `data/input/corrected.csv` |
| Model load failed     | Check if model file exists at `Models/savedmodel1.zip`                  |
| Docker errors (mount issue) | Use full absolute paths in `-v` argument for Docker              |
| CSV format errors     | Ensure your CSV has headers: `User,Computer,Time,Date`                  |
| No valid data rows    | Make sure your time format is correct: `yyyy-MM-dd HH:mm`               |

---

## ✅ Final Project Structure Example

```
AnomalyDetectionApp/
├── Models/               # Where trained models get saved  
├── data/  
│   ├── input/corrected.csv     # Prediction data  
│   └── training/corrected.csv  # Training data  
├── Config/  
│   └── AppSettings.json        # Config file you created  
├── RunTraining.bat             # Optional for Windows  
└── RunPrediction.bat           # Optional for Windows  
```

---

## ✅ Need Help? Contact Dipankar

If something doesn't work:

**Email:** dipankarthirupathi@gmail.com

When you email:
- Include the error message from terminal
- Mention your OS (Windows/Linux/Mac)
- Attach your `AppSettings.json` file
