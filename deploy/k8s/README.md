# Kubernetes: ручне розгортання

Секрети НЕ зберігаються в репозиторії: створюються командами kubectl.

## Порядок
1. `kubectl apply -f configmap.yaml`
2. Створити Secrets (див. нижче)
3. `kubectl apply -f catalog-api-deployment.yaml -f catalog-api-service.yaml`
4. `kubectl apply -f gateway-deployment.yaml -f gateway-service.yaml`

## Secrets (PowerShell)
Спочатку задати змінні оточення з реальними значеннями (рядки підключення та ключ JWT):

```powershell
$env:CATALOG_DB  = "<рядок підключення до CatalogDb>"
$env:RABBIT_CONN = "<рядок підключення до RabbitMQ>"
$env:REDIS_CONN  = "<рядок підключення до Redis>"
$env:JWT_KEY     = "<ключ підпису JWT, мінімум 32 символи>"

kubectl create secret generic catalog-secrets `
  --from-literal=ConnectionStrings__CatalogDb="$env:CATALOG_DB" `
  --from-literal=ConnectionStrings__messaging="$env:RABBIT_CONN" `
  --from-literal=ConnectionStrings__redis="$env:REDIS_CONN" `
  --from-literal=Jwt__SecretKey="$env:JWT_KEY"

kubectl create secret generic gateway-secrets `
  --from-literal=Jwt__SecretKey="$env:JWT_KEY"
```

> Secrets у Kubernetes лише закодовані в Base64, а не зашифровані.
> У production: шифрування etcd та зовнішні сховища (Vault, Azure Key Vault).