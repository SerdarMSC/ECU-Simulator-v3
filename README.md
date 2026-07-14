## 📖 A Windows Forms application that simulates a vehicle ECU (Electronic Control Unit) over a physical CAN bus, enabling OBD-II diagnostic tools to connect and read live sensor data without a real car.

## 📖 Gerçek bir araç olmaksızın OBD-II arıza tespit cihazlarının bağlanıp canlı sensör verilerini okumasını sağlayan; fiziksel bir CAN veri yolu üzerinden araç ECU'sunu (Elektronik Kontrol Ünitesi) simüle eden bir Windows Forms uygulaması.

<img width="480" height="480" alt="image" src="https://github.com/user-attachments/assets/314b50b2-5bfd-4675-b2d1-a759e327b3a3" />


## 📖 Overview
This project provides a fully interactive ECU simulator designed for testing OBD-II diagnostic applications (such as Torque, Car Scanner, or custom Android APKs) in a lab environment. It connects to a physical CAN bus via an **Arduino** board equipped with an **MCP2515 CAN module**, bridging the gap between your PC and an **ELM327** Bluetooth adapter.
The simulator responds to standard OBD-II requests (`0x7DF`) with realistic ECU data (`0x7E8`) and supports manual control over all major sensor parameters, fault code injection, and real-time CAN traffic monitoring.

## 📖 Genel Bakış
Bu proje, laboratuvar ortamında OBD-II teşhis uygulamalarını (Torque, Car Scanner veya özel Android APK'ları gibi) test etmek üzere tasarlanmış, tam etkileşimli bir ECU simülatörü sunar. Sistem, **MCP2515 CAN modülü** ile donatılmış bir **Arduino** kartı aracılığıyla fiziksel bir CAN veri yoluna bağlanarak, bilgisayarınız ile bir **ELM327** Bluetooth adaptörü arasında köprü görevi görür.
Simülatör, standart OBD-II isteklerine (`0x7DF`) gerçekçi ECU verileriyle (`0x7E8`) yanıt verir; ayrıca tüm temel sensör parametrelerinin manuel kontrolünü, arıza kodu enjeksiyonunu ve gerçek zamanlı CAN trafiği izlemeyi destekler.

<img width="986" height="813" alt="image" src="https://github.com/user-attachments/assets/509e9b0f-f4c7-4bfe-bb43-db485dd91efa" />

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
can-usb.ino

