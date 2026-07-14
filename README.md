## 📖 A Windows Forms application that simulates a vehicle ECU (Electronic Control Unit) over a physical CAN bus, enabling OBD-II diagnostic tools to connect and read live sensor data without a real car.

## 📖 Gerçek bir araç olmaksızın OBD-II arıza tespit cihazlarının bağlanıp canlı sensör verilerini okumasını sağlayan; fiziksel bir CAN veri yolu üzerinden araç ECU'sunu (Elektronik Kontrol Ünitesi) simüle eden bir Windows Forms uygulaması.

## 📖 Overview
This project provides a fully interactive ECU simulator designed for testing OBD-II diagnostic applications (such as Torque, Car Scanner, or custom Android APKs) in a lab environment. It connects to a physical CAN bus via an **Arduino** board equipped with an **MCP2515 CAN module**, bridging the gap between your PC and an **ELM327** Bluetooth adapter.
The simulator responds to standard OBD-II requests (`0x7DF`) with realistic ECU data (`0x7E8`) and supports manual control over all major sensor parameters, fault code injection, and real-time CAN traffic monitoring.

## 📖 Genel Bakış
Bu proje, laboratuvar ortamında OBD-II teşhis uygulamalarını (Torque, Car Scanner veya özel Android APK'ları gibi) test etmek üzere tasarlanmış, tam etkileşimli bir ECU simülatörü sunar. Sistem, **MCP2515 CAN modülü** ile donatılmış bir **Arduino** kartı aracılığıyla fiziksel bir CAN veri yoluna bağlanarak, bilgisayarınız ile bir **ELM327** Bluetooth adaptörü arasında köprü görevi görür.
Simülatör, standart OBD-II isteklerine (`0x7DF`) gerçekçi ECU verileriyle (`0x7E8`) yanıt verir; ayrıca tüm temel sensör parametrelerinin manuel kontrolünü, arıza kodu enjeksiyonunu ve gerçek zamanlı CAN trafiği izlemeyi destekler.

| Component | Recommended Model |
|-----------|-------------------|
| **Microcontroller** | Arduino Uno / Leonardo / Nano |
| **CAN Module** | MCP2515 (with TJA1050 or MCP2551 transceiver) |
| **CAN Bus Termination** | 120Ω resistor (both ends) |
| **OBD-II Adapter** | ELM327 Bluetooth (for mobile testing) |

## ⚙️ Arduino Firmware (SLCAN/CanHacker)
Upload the following firmware to your Arduino using the **CanHacker** or **SLCAN** library:
**CanHacker** veya **SLCAN** kütüphanesini kullanarak aşağıdaki aygıt yazılımını Arduino'nuza yükleyin:

---------- ino file / ino dosyası -----------

#include <can.h>
#include <mcp2515.h>
#include <CanHacker.h>
#include <CanHackerLineReader.h>
#include <SPI.h>

const int SPI_CS_PIN = 10;
const int INT_PIN = 2;

CanHackerLineReader *lineReader = NULL;
CanHacker *canHacker = NULL;

void setup() {
    Serial.begin(115200);
    while (!Serial);
    SPI.begin();

    Stream *interfaceStream = &Serial;
    Stream *debugStream = &Serial;

    canHacker = new CanHacker(interfaceStream, debugStream, SPI_CS_PIN);
    canHacker->setClock(MCP_8MHZ);   // 8MHZ İÇİN (EĞER BOARD ÜZERİNDE 16MHZ KRİSTAL VARSA : canHacker->setClock(MCP_16MHZ) )
    lineReader = new CanHackerLineReader(canHacker);
    pinMode(INT_PIN, INPUT);
}

void loop() {
    if (digitalRead(INT_PIN) == LOW) {
        CanHacker::ERROR error = canHacker->processInterrupt();
        if (error != CanHacker::ERROR_OK) {
            Serial.print("Interrupt Error: ");
            Serial.println((int)error);
        }
    }

    CanHacker::ERROR error = lineReader->process();
    if (error != CanHacker::ERROR_OK && error != CanHacker::ERROR_UNKNOWN_COMMAND) {
        Serial.print("LineReader Error: ");
        Serial.println((int)error);
    }
}
