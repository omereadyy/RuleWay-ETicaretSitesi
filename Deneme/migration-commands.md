# Migration Komutları

RuleWay E-Ticaret projesinde veritabanını oluşturmak için bu komutları sırasıyla çalıştırın:

## 1. Migration Oluştur
```bash
cd Deneme
dotnet ef migrations add InitialCreate
```

## 2. Veritabanını Güncelle
```bash
dotnet ef database update
```

## 3. Projeyi Çalıştır
```bash
dotnet run
```

## 4. Swagger UI'yi Kontrol Et
Tarayıcıda şu adresi açın:
- https://localhost:7xxx/swagger (HTTPS)
- http://localhost:5xxx/swagger (HTTP)

## Veritabanı Bilgileri
- **Production DB**: RuleWayECommerce
- **Development DB**: RuleWayECommerce_Dev  
- **Server**: (localdb)\mssqllocaldb
- **Seed Data**: 3 Kategori otomatik eklenecek

## Seed Data:
1. Elektronik (Min Stock: 5)
2. Giyim (Min Stock: 10)  
3. Kitap (Min Stock: 3)

## API Endpoints:
- `GET /api/Products` - Tüm ürünler
- `GET /api/Products/{id}` - Tek ürün
- `POST /api/Products` - Ürün oluştur
- `PUT /api/Products/{id}` - Ürün güncelle
- `DELETE /api/Products/{id}` - Ürün sil
- `GET /api/Products/filter?searchKeyword=&minStock=&maxStock=` - Filtreleme

- `GET /api/Categories` - Tüm kategoriler
- `GET /api/Categories/{id}` - Tek kategori
- `POST /api/Categories` - Kategori oluştur
- `PUT /api/Categories/{id}` - Kategori güncelle
- `DELETE /api/Categories/{id}` - Kategori sil