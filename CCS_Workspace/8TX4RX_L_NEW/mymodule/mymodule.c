/* --COPYRIGHT--,BSD
 * Copyright (c) 2017, Texas Instruments Incorporated
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * *  Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * *  Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * *  Neither the name of Texas Instruments Incorporated nor the names of
 *    its contributors may be used to endorse or promote products derived
 *    from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * --/COPYRIGHT--*/
// #############################################################################
//
//! \file   Backchannel_UART_demo.c
//
//  Group:          MSP
//  Target Devices: MSP430FR2633
//
//  (C) Copyright 2018, Texas Instruments, Inc.
// #############################################################################
// TI Release: 1.83.00.05
// Release Date: May 15, 2020
// #############################################################################

//*****************************************************************************
// Includes
//*****************************************************************************

#include "mymodule.h"

//*****************************************************************************
// Definitions
//*****************************************************************************

//*****************************************************************************
// Global Variables
//*****************************************************************************

//*****************************************************************************
// Function Prototypes
//*****************************************************************************

//*****************************************************************************
// Function Implementations
//*****************************************************************************
#define NUM_BUTTONS     2
#define FRAMES_PER_BTN  12   // giả sử max cycles = 12, điều chỉnh theo thực tế
#define FRAME_SIZE 8

static uint8_t tx_buf[NUM_BUTTONS][FRAMES_PER_BTN][FRAME_SIZE];
static uint8_t tx_frame_counts[NUM_BUTTONS] = {0};   // đếm riêng cho từng button
static volatile bool tx_pending_flags[NUM_BUTTONS] = {false};


#define RX_BUFFER_SIZE 64
#define RX_RING_SIZE 128
static uint8_t rx_buffer[RX_BUFFER_SIZE];
static uint8_t rx_index = 0;
static volatile uint8_t rx_ring[RX_RING_SIZE];
static volatile uint8_t rx_head = 0; // write index (ISR)
static volatile uint8_t rx_tail = 0; // read index (main)

#define STX 0x02
#define ETX 0x03

extern volatile bool isFrozen;



static void prepare_tx_for_button(tSensor *pSensor, uint8_t btn_idx, uint8_t button_id)
{
    uint8_t *count_ptr = &tx_frame_counts[btn_idx];
    *count_ptr = 0;
    uint8_t iCycle ;
    for ( iCycle = 0; iCycle < pSensor->ui8NrOfCycles; iCycle++)
    {
        if (*count_ptr >= FRAMES_PER_BTN) break;

        uint8_t *frame = tx_buf[btn_idx][*count_ptr];

        frame[0] = STX;
        frame[1] = button_id;
        frame[2] = iCycle;
        uint8_t iElement;
        for (iElement = 0; iElement < 2; iElement++)
        {
            if (iElement < pSensor->pCycle[iCycle]->ui8NrOfElements)
            {
                uint16_t fc  = pSensor->pCycle[iCycle]->pElements[iElement]->filterCount.ui16Natural;
                uint16_t lta = pSensor->pCycle[iCycle]->pElements[iElement]->LTA.ui16Natural;
                uint16_t delta = (fc > lta) ? (fc - lta) : (lta - fc);

                frame[3 + iElement * 2] = (uint8_t)(delta >> 8);
                frame[4 + iElement * 2] = (uint8_t)(delta & 0xFF);
            }
            else
            {
                frame[3 + iElement * 2] = 0;
                frame[4 + iElement * 2] = 0;
            }
        }

        frame[FRAME_SIZE - 1] = ETX;
        (*count_ptr)++;
    }

    tx_pending_flags[btn_idx] = (*count_ptr > 0);
}



// callback functions for buttons
void btn00_callback(tSensor *pSensor) { prepare_tx_for_button(pSensor, 0, 0x00); }
void btn01_callback(tSensor *pSensor) { prepare_tx_for_button(pSensor, 1, 0x01); }

void transmit_pending_data(void)
{   
    uint8_t btn;
    for (btn = 0; btn < NUM_BUTTONS; btn++)
    {
        if (!tx_pending_flags[btn]) continue;
        uint8_t i ;
        for ( i = 0; i < tx_frame_counts[btn]; i++)
        {
            UART_transmitBuffer(tx_buf[btn][i], FRAME_SIZE);
            __delay_cycles(200000);
        }

        tx_frame_counts[btn] = 0;
        tx_pending_flags[btn] = false;
    }
}

void disable_handling(void) {
  isFrozen = true;
  static uint8_t frame[8];
  frame[0] = STX;
  frame[1] = 'D';
  frame[2] = 'I';
  frame[3] = 'S';
  frame[4] = 'A';
  frame[5] = 'L';
  frame[6] = 'B';
  frame[7] = ETX;
  UART_transmitBuffer(frame, 8);
}

void enable_handling(void) {
    isFrozen = false;

  static uint8_t frame[8];
  frame[0] = STX;
  frame[1] = 'E';
  frame[2] = 'N';
  frame[3] = 'A';
  frame[4] = 'B';
  frame[5] = 'L';
  frame[6] = 'E';
  frame[7] = ETX;
  UART_transmitBuffer(frame, 8);
}

void recalib_handling(void) {

  // Recalibrate toàn bộ UI - cách đơn giản nhất
  CAPT_calibrateUI(&g_uiApp);

  static uint8_t frame[8];
  frame[0] = STX;
  frame[1] = 'R';
  frame[2] = 'E';
  frame[3] = 'S';
  frame[4] = 'T';
  frame[5] = 'O';
  frame[6] = 'K';
  frame[7] = ETX;
  UART_transmitBuffer(frame, 8);
}

void verify_handling(void) {

  static uint8_t frame[8];
  frame[0] = STX;
  frame[1] = 'M';
  frame[2] = 'R';
  frame[3] = 0x00;
  frame[4] = 0x00;
  frame[5] = 0x00;
  frame[6] = 0x00;
  frame[7] = ETX;
  UART_transmitBuffer(frame, 8);

}

void unknown_handling(void) {

  static uint8_t frame[8];
  frame[0] = STX;
  frame[1] = 'U';
  frame[2] = 'N';
  frame[3] = 'K';
  frame[4] = 'N';
  frame[5] = 'O';
  frame[6] = 'W';
  frame[7] = ETX;
  UART_transmitBuffer(frame, 8);
}


typedef enum {
  CMD_UNKNOWN = 0,
  CMD_RESET,
  CMD_VERIFY,
  CMD_DISABLE,
  CMD_ENABLE
} command_t;


command_t parse_command(char *cmd) {
  if (strcmp(cmd, "RESET") == 0)
    return CMD_RESET;

  if (strcmp(cmd, "DISABLE") == 0)
    return CMD_DISABLE;
  if (strcmp(cmd, "ENABLE") == 0)
    return CMD_ENABLE;
  if (strcmp(cmd, "VERIFY") == 0)
    return CMD_VERIFY;
  return CMD_UNKNOWN;
}

void process_command(char *cmd) {
  command_t command = parse_command(cmd);

  switch (command) {
  case CMD_RESET:
    recalib_handling();
    break;

  case CMD_DISABLE:
    disable_handling();
    break;
  case CMD_ENABLE:
    enable_handling();
    break;

  case CMD_VERIFY:
    verify_handling();
    break;

  case CMD_UNKNOWN:
  default:
    unknown_handling();
    break;
  }
}

void uart_rx_callback(uint8_t data_byte) { // Giữ tên nếu driver yêu cầu
uint8_t next_head = (rx_head + 1) & (RX_RING_SIZE - 1);
  if (next_head != rx_tail) { // Không full
    rx_ring[rx_head] = data_byte;
    rx_head = next_head;
  } // else overrun → có thể flag error
}

uint8_t uart_available(void)
{
    return (rx_head - rx_tail) & (RX_RING_SIZE - 1);
}


int16_t uart_read(void)
{
    if (rx_head == rx_tail)
        return -1;  // không có data

    uint8_t data = rx_ring[rx_tail];
    rx_tail = (rx_tail + 1) & (RX_RING_SIZE - 1);

    return data;
}


void process_rx(void)
{
    while (uart_available())
    {
        int16_t data = uart_read();
        if (data < 0)
            break;

        uint8_t byte = (uint8_t)data;

        // Bỏ qua LF để tránh double trigger CRLF
        if (byte == '\n')
            continue;

        if (byte == '\r')
        {
            rx_buffer[rx_index] = '\0';
            process_command((char *)rx_buffer);
            rx_index = 0;
        }
        else if (rx_index < RX_BUFFER_SIZE - 1)
        {
            rx_buffer[rx_index++] = byte;
        }
        else
        {
            // Chuỗi quá dài → reset tránh lỗi
            rx_index = 0;
        }
    }
}
// SMCLK = 2 MHz, Baud = 9600
const tUARTPort UARTPort = {
    .pbReceiveCallback = &uart_rx_callback,
    .pbErrorCallback   = NULL,

    .peripheralParameters.selectClockSource =
        EUSCI_A_UART_CLOCKSOURCE_SMCLK,

    // UCBRx = 13
    .peripheralParameters.clockPrescalar = 13,

    // UCBRFx = 5  
    .peripheralParameters.firstModReg = 5,

    // UCBRSx = 0x49 (đúng)
    .peripheralParameters.secondModReg = 0x49,

    .peripheralParameters.parity =
        EUSCI_A_UART_NO_PARITY,

    .peripheralParameters.msborLsbFirst =
        EUSCI_A_UART_LSB_FIRST,

    .peripheralParameters.numberofStopBits =
        EUSCI_A_UART_ONE_STOP_BIT,

    .peripheralParameters.uartMode =
        EUSCI_A_UART_MODE,

    .peripheralParameters.overSampling =
        EUSCI_A_UART_OVERSAMPLING_BAUDRATE_GENERATION
};
