# HexAI 🧠

HexAI, tamamen C# ve .NET ile sıfırdan geliştirilmiş kapsamlı bir Yapay Zeka (AI) ve Makine Öğrenmesi framework'üdür. Kendi içinde SIMD ile optimize edilmiş Matris/Tensor matematiğini, sinir ağı (Neural Network) katmanlarını ve Transformer (GPT) mimarilerini barındırır.

## 🚀 Özellikler

### ⚙️ Çekirdek Motor (`HexAI.Core` & `HexAI.NeuralNetwork`)
* **Özel Matematik ve Tensörler:** SIMD hızlandırması ile yüksek performanslı Matris ve Tensör işlemleri.
* **Derin Öğrenme Katmanları:** Dense, Conv2D, RNN, LSTM, Dropout, MaxPooling ve LayerNorm desteği.
* **Attention (Dikkat) Mekanizmaları:** Multi-Head Attention, Positional Encoding ve Transformer mimarisi katmanları.
* **Optimizer ve Kayıp Fonksiyonları:** Adam, SGD optimizasyonları ile CrossEntropy, MSE vb. kayıp hesaplamaları.
* **Üretken Modeller (Generative):** GAN (Generative Adversarial Network) altyapısı.

### 🗣️ Doğal Dil İşleme (`HexAI.NLP`)
* **Özel Tokenizer & Sözlük (Vocabulary):** Kelime ve alt-kelime tabanlı metin ayrıştırma.
* **Generative AI (Üretken YZ):** Metin üretimi için sıfırdan yazılmış Mini-GPT ve Large-GPT modelleri.
* **Niyet Sınıflandırma (Intent Classification):** Sohbet botları ve asistanlar için sinir ağı tabanlı niyet tanıma sistemi.

### 💻 Arayüzler ve Entegrasyon
* **HexAI.WebUI:** Blazor ile geliştirilmiş modern web arayüzü.
* **HexAI.Desktop:** WPF ile geliştirilmiş masaüstü uygulaması.
* **HexAI.CLI:** Hızlı test ve etkileşim için komut satırı aracı.
* **HexAI.Assistant:** PowerShell yetenekleri vb. destekleyen yapay zeka asistan modülü.

## 📂 Proje Yapısı

* `src/HexAI.Core/`: Çekirdek matematik, matris işlemleri ve kayıp fonksiyonları.
* `src/HexAI.NeuralNetwork/`: Derin öğrenme katmanları, ağ mimarisi ve optimizer'lar.
* `src/HexAI.NLP/`: NLP, tokenizasyon ve dil modelleri.
* `src/HexAI.Desktop/`: WPF masaüstü uygulaması.
* `src/HexAI.WebUI/`: Blazor web uygulaması.
* `cli/HexAI.CLI/`: Komut satırı arayüzü.
* `demo/HexAI.Demo/`: Framework'ün yeteneklerini (SIMD, XOR ağı, Transformer) gösteren demo konsol uygulaması.
* `tests/HexAI.Tests/`: Birim testleri (Unit Tests).

## 🛠️ Kurulum ve Çalıştırma

Projeyi bilgisayarınıza indirip örnek demoyu çalıştırmak için:

```bash
# Depoyu klonlayın
git clone https://github.com/HexStereo/HexAI.git

# Demo klasörüne gidin
cd HexAI/demo/HexAI.Demo

# Demoyu çalıştırın
dotnet run
