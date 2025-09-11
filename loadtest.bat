@echo off
for /L %%i in (1,1,500) do (
  curl -s -X POST http://localhost:8080/api/order ^
    -H "Content-Type: application/json" ^
    -d "{\"id\":\"o-%%i\",\"amount\":99}"
)