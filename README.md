# [StockPulse](https://stockpulse.ahmetmahirdemirelli.com)

* Redis Pub/Sub ve SignalR tabanlı gerçek zamanlı, yüksek performanslı merkezi izleme dashboard uygulaması.
* A real-time, high-performance centralized monitoring dashboard built with Redis Pub/Sub and SignalR.

---

## 🛠️ Tech Stack / Teknolojiler

### 🇹🇷 Türkçe
### Backend (.NET 10)
* **Redis (Pub/Sub):** Yüksek hızlı veri üretimi ile dağıtım katmanı arasında mesaj kuyruğu (Message Broker) görevi ve veri önbelleğe alma.
* **SignalR:** Token doğrulamalı, çift yönlü canlı log akışı (WebSockets).
* **Background Services:** Redis kanallarını asenkron dinleyen worker yapısı, simülatör yönetimi ve veritabanı kota koruma mekanizmaları.
* **JWT Authentication:** Güvenli token tabanlı kimlik doğrulama yönetimi.

### Frontend (Angular 21)
* **Angular Signals:** Yüksek frekanslı veri akışı için reaktif durum yönetimi.
* **Tailwind CSS v4:** Koyu/açık tema uyumlu minimal arayüz tasarımı.
* **RxJS Streams:** Canlı tablolar için bellek korumalı kuyruk yönetimi (`switchMap`, `takeUntil`).

### English
### Backend (.NET 10)
* **Redis (Pub/Sub):** Serves as a high-throughput message broker between data generation and distribution layers, alongside caching.
* **SignalR:** Token-authenticated, bi-directional live log streaming (WebSockets).
* **Background Services:** Asynchronous workers dedicated to listening to Redis channels, handling log simulation, and database quota enforcement.
* **JWT Authentication:** Secure token-based authentication management.

### Frontend (Angular 21)
* **Angular Signals:** Reactive state management optimized for high-frequency data streams.
* **Tailwind CSS v4:** Minimal user interface design with native dark/light mode support.
* **RxJS Streams:** Memory-safe adaptive queue management (switchMap, takeUntil) for live tables.

---

## ⚙️ Core Logic / Temel Mantık

### 🇹🇷 Türkçe
* **Reaktif Akış Hattış:** ÜSimülatör veriyi üretir $\rightarrow$ Veri anlık olarak Redis Pub/Sub kanalına fırlatılır $\rightarrow$ RedisRouteToSignalRWorker bu kanalı dinleyerek gelen veriyi yakalar $\rightarrow$ SignalR Hub üzerinden tüm aktif istemcilere anlık olarak dağıtır.
* **Bellek Optimizasyonu:** Yüksek hızlı akışlarda tarayıcının kilitlenmesini önlemek için kullanıcı sınırına (0-500) göre eski loglar diziden (`pop`) atılır.
* **Otomatik Durdurma (Auto-Stop):** Hub bağlantısı kesildiğinde (aktif kullanıcı kalmadığında) arka plan simülatörü sunucu kaynaklarını korumak için otomatik durdurulur.

###  English
* **Reactive Data Pipeline:** Simulator generates data $\rightarrow$ Data is immediately published to a Redis Pub/Sub channel $\rightarrow$ RedisRouteToSignalRWorker listens to the channel asynchronously $\rightarrow$ Broadcasts the payload to all connected clients via SignalR Hub.
* **Memory Protection:** To prevent browser lagging, the frontend limits the array size (0-500) and drops older logs using `pop()`.
* **Resource Optimization (Auto-Stop):** The background simulator automatically stops when no active clients are connected to the SignalR Hub.
