@echo off
echo Loading Docker image...
docker load -i Docker\anomaly-detector.tar

echo Running training...
docker run --rm ^
           -v "%CD%\Models:/data/models" ^
           -v "%CD%\Data:/app/data" ^
           anomaly-detector train /app/data/training/corrected.csv

pause