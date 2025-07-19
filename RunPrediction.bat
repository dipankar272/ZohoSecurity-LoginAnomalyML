@echo off
echo Loading Docker image...
docker load -i Docker\anomaly-detector.tar

echo Running prediction...
docker run --rm ^
           -v "%CD%\Models:/data/models" ^
           -v "%CD%\Data:/app/data" ^
           anomaly-detector predict /app/data/input/corrected.csv 1

pause