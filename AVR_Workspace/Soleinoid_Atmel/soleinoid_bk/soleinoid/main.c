#include <avr/io.h>
#include <util/delay.h>
#include <stdint.h>

#define F_CPU 11059200UL
#define BAUD 9600
#define MYUBRR ((F_CPU / (16UL * BAUD)) - 1)


// Pin definitions
#define DATA_PORT       PORTA   // Common data bus
#define DATA_DDR        DDRA
#define DATA_PIN        PINA

#define CS1     PC7
#define CS2     PC6
#define CS3     PC5
#define OUT_EN  PC4

// UART Frame structure
#define HEADER_BYTE1    0x44
#define HEADER_BYTE2    0x45
#define FOOTER_BYTE     0x56
#define FRAME_LENGTH    10      // Header(2) + Length(1) + Command(1) + Data(4) + CRC(1) + Footer(1)
#define ACK_FRAME_LENGTH 6      // Header(2) + Length(1) + Status(1) + CRC(1) + Footer(1)

// Command definitions
#define OUTPUT_CMD      0x53

// Response codes
#define ACK_SUCCESS     0x01
#define ACK_CRC_ERROR   0x02
#define ACK_FORMAT_ERR  0x03

// Global variables
uint8_t uart_buffer[FRAME_LENGTH];
uint8_t uart_index = 0;
uint8_t frame_ready = 0;
uint8_t output_values[3] = {0}; // Current values for 24 outputs

uint8_t xor8_checksum(const uint8_t* data, uint16_t len) {
	uint8_t checksum = 0;
	for (uint16_t i = 0; i < len; i++) {
		checksum ^= data[i];
	}
	return checksum;
}

// Clock-aware delay function
void clock_aware_delay_us(uint16_t us) {
	for (uint16_t i = 0; i < us; i++) {
		_delay_us(1);  // Will be adjusted by F_CPU define
	}
}

void init_ports(void) {
	// Disable JTAG
	MCUCSR = (1 << JTD);
	MCUCSR = (1 << JTD);
	
	// Set data port as output
	DATA_DDR = 0xFF;
	DATA_PORT = 0x00;
	
	// Configure control pins
	DDRC |= (1 << CS1) | (1 << CS2) | (1 << CS3) | (1 << OUT_EN);
	
	// Disable all chip selects and enable output (active low)
	PORTC |= (1 << CS1) | (1 << CS2) | (1 << CS3);
	PORTC &= ~(1 << OUT_EN);
}

void init_uart(void) {
	// Calculate baud rate based on current F_CPU
	uint16_t ubrr = MYUBRR;
	UBRR0H = (uint8_t)(ubrr >> 8);
	UBRR0L = (uint8_t)ubrr;
	
	// Enable receiver and transmitter (no RX interrupt)
	UCSR0B = (1 << RXEN0) | (1 << TXEN0);
	
	// Frame format: 8 data bits, 1 stop bit, no parity
	UCSR0C = (1 << URSEL0) | (1 << UCSZ01) | (1 << UCSZ00);
}

uint8_t reverse_bits(uint8_t b) {
	uint8_t reversed = 0;
	for (int i = 0; i < 8; i++) {
		reversed = (reversed << 1) | (b & 1);
		b >>= 1;
	}
	return reversed;
}

void write_to_ic(uint8_t ic_num, uint8_t data) {
	// Put data on the bus
	DATA_PORT = data;
	
	// Select target IC (active low)
	switch(ic_num) {
		case 1:
		PORTC &= ~(1 << CS1);
		clock_aware_delay_us(1);
		PORTC |= (1 << CS1);
		break;
		case 2:
		PORTC &= ~(1 << CS2);
		clock_aware_delay_us(1);
		PORTC |= (1 << CS2);
		break;
		case 3:
		PORTC &= ~(1 << CS3);
		clock_aware_delay_us(1);
		PORTC |= (1 << CS3);
		break;
	}
}

void update_outputs(void) {
	// Write values to each IC
	write_to_ic(1, output_values[0]);  // IC1 controls outputs 1-8
	write_to_ic(2, output_values[1]);  // IC2 controls outputs 9-16
	write_to_ic(3, output_values[2]);  // IC3 controls outputs 17-24
}

uint8_t validate_frame(uint8_t *frame) {
	// Check header and footer
	if(frame[0] != HEADER_BYTE1 || frame[1] != HEADER_BYTE2 ||
	frame[FRAME_LENGTH-1] != FOOTER_BYTE) {
		return ACK_FORMAT_ERR;
	}
	
	// Verify data length byte
	if(frame[2] != 0x06) {  // Should always be 6 for this command
		return ACK_FORMAT_ERR;
	}
	
	// Verify command byte
	if(frame[3] != OUTPUT_CMD) {
		return ACK_FORMAT_ERR;
	}
	
	// Calculate CRC (for all bytes except footer)
	uint8_t crc = xor8_checksum(frame, FRAME_LENGTH-2);
	
	// Compare with received CRC (second to last byte)
	if(crc != frame[FRAME_LENGTH-2]) {
		return ACK_CRC_ERROR;
	}
	
	return ACK_SUCCESS;
}

void send_response(uint8_t status) {
	uint8_t response[ACK_FRAME_LENGTH];
	
	// Build response frame
	response[0] = HEADER_BYTE1;    // Header
	response[1] = HEADER_BYTE2;
	response[2] = 0x02;            // Length (status + CRC = 2 bytes)
	response[3] = status;          // Status code
	response[4] = xor8_checksum(response, 4); // CRC
	response[5] = FOOTER_BYTE;     // Footer
	
	// Send response
	for(uint8_t i = 0; i < ACK_FRAME_LENGTH; i++) {
		while(!(UCSR0A & (1 << UDRE0)));
		UDR0 = response[i];
	}
}

void process_frame(uint8_t *frame) {
	// Only use Data1-Data3 (frame[4]-frame[6]), Data4 (frame[7]) is reserved
	output_values[0] = frame[4];  // Outputs 1-8
	output_values[1] = frame[5];  // Outputs 9-16
	output_values[2] = frame[6];  // Outputs 17-24
	
	// Apply new values
	update_outputs();
}

void uart0_send(uint8_t data) {
	while (!(UCSR0A & (1 << UDRE0)));
	UDR0 = data;
}

void receive_frame(void) {
	static uint8_t receiving = 0;
	static uint8_t bytes_received = 0;
	
	if((UCSR0A & (1 << RXC0))) {
		uint8_t data = UDR0;	
		
		//uart0_send(data);	
		
		if(!receiving) {
			// Wait for header
			if(bytes_received == 0 && data == HEADER_BYTE1) {
				uart_buffer[bytes_received++] = data;
			}
			else if(bytes_received == 1 && data == HEADER_BYTE2) {
				uart_buffer[bytes_received++] = data;
				receiving = 1;
			}
			else {
				bytes_received = 0;  // Reset if header not matched
			}
			
		}
		else {
			// Store received byte
			uart_buffer[bytes_received++] = data;
			
			// Check if complete frame received
			if(bytes_received >= FRAME_LENGTH) {
				receiving = 0;
				bytes_received = 0;
				frame_ready = 1;
			}
		}
	}
}

int main(void) {
	// System initialization
	init_ports();
	init_uart();

	// Initialize default values
	output_values[0] = 0x00;
	output_values[1] = 0x00;
	output_values[2] = 0x00;
	update_outputs();
	
	while(1) {
		// Poll for incoming UART data
		
		receive_frame();
		
		// Process frame if ready
		if(frame_ready) {
			uint8_t validation_result = validate_frame(uart_buffer);
			// Send response
			send_response(validation_result);
			//
			// Process only if valid
			if(validation_result == ACK_SUCCESS) {
				process_frame(uart_buffer);
			}
			// Reset flag
			frame_ready = 0;
		}
	}
	
	return 0;
}