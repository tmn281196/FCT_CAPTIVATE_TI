#include <avr/io.h>
#include <util/delay.h>

#define DATA_PORT PORTA // Port A (PA0-PA7) cho D0-D7
#define CLOCK_PIN_1 PC7   
#define CLOCK_PIN_2 PC6    
#define CLOCK_PIN_3 PC5   

#define OE_PIN    PC4   // PC4 cho OE

void init_ports() {
	MCUCSR = (1 << JTD);
	MCUCSR = (1 << JTD);
	DDRA = 0xFF;
	DDRC = 0xFF;
	PORTC |= (1 << OE_PIN);
	PORTC &= ~(1 << CLOCK_PIN_1);
	PORTC &= ~(1 << CLOCK_PIN_2);
	PORTC &= ~(1 << CLOCK_PIN_3);

}

void send_data_1(uint8_t data) {
	DATA_PORT = data;
	PORTC &= ~(1 << CLOCK_PIN_1); // CP = LOW
	_delay_us(1);
	PORTC |= (1 << CLOCK_PIN_1);  // CP = HIGH
	_delay_us(1);
	PORTC &= ~(1 << CLOCK_PIN_1); // CP = LOW
}
void send_data_2(uint8_t data) {
	DATA_PORT = data;
	PORTC &= ~(1 << CLOCK_PIN_2); // CP = LOW
	_delay_us(1);
	PORTC |= (1 << CLOCK_PIN_2);  // CP = HIGH
	_delay_us(1);
	PORTC &= ~(1 << CLOCK_PIN_2); // CP = LOW
}

void send_data_3(uint8_t data) {
	DATA_PORT = data;
	PORTC &= ~(1 << CLOCK_PIN_3); // CP = LOW
	_delay_us(1);
	PORTC |= (1 << CLOCK_PIN_3);  // CP = HIGH
	_delay_us(1);
	PORTC &= ~(1 << CLOCK_PIN_3); // CP = LOW
}
void enable_output() {
	PORTC &= ~(1 << OE_PIN);
}

void disable_output() {
	PORTC |= (1 << OE_PIN);
}

int main(void) {
	init_ports();
	
	enable_output();
	send_data_1(0x00);
	send_data_2(0xEE);
	send_data_3(0xFF);

	return 0;
}