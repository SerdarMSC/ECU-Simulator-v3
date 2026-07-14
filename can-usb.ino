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
    canHacker->setClock(MCP_8MHZ);   // ★ 8 MHZ İÇİN ★
    // canHacker->enableLoopback(); // KAPALI OLSUN

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