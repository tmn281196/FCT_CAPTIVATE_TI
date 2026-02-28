#define STX 0x02
#define ETX 0x03

// Giá trị gửi đi
// 0x00 = STOP (đã dừng ở trên hoặc dưới)
// 0x01 = UP (đang đi lên)
// 0x02 = DOWN (đang đi xuống)
// 0xFF = ERROR

const int REED_MAIN_TOP    = 22;
const int REED_MAIN_BOTTOM = 23;

// Container reeds
const int REED_CON1_TOP    = 24; const int REED_CON1_BOTTOM = 25;
const int REED_CON2_TOP    = 26; const int REED_CON2_BOTTOM = 27;
const int REED_CON3_TOP    = 28; const int REED_CON3_BOTTOM = 29;
const int REED_CON4_TOP    = 30; const int REED_CON4_BOTTOM = 31;

byte mainDirection = 0x00;          // Giá trị hiện tại gửi đi (ban đầu STOP)
byte lastMovingDirection = 0x00;    // Hướng di chuyển cuối cùng (giữ khi ở giữa)

bool prevMainTop    = false;
bool prevMainBottom = false;

void setup() {
  Serial.begin(9600);

  pinMode(REED_MAIN_TOP,    INPUT_PULLUP);
  pinMode(REED_MAIN_BOTTOM, INPUT_PULLUP);
  pinMode(REED_CON1_TOP,    INPUT_PULLUP);
  pinMode(REED_CON1_BOTTOM, INPUT_PULLUP);
  pinMode(REED_CON2_TOP,    INPUT_PULLUP);
  pinMode(REED_CON2_BOTTOM, INPUT_PULLUP);
  pinMode(REED_CON3_TOP,    INPUT_PULLUP);
  pinMode(REED_CON3_BOTTOM, INPUT_PULLUP);
  pinMode(REED_CON4_TOP,    INPUT_PULLUP);
  pinMode(REED_CON4_BOTTOM, INPUT_PULLUP);

  // Đọc trạng thái ban đầu
  bool currTop    = (digitalRead(REED_MAIN_TOP) == HIGH);
  bool currBottom = (digitalRead(REED_MAIN_BOTTOM) == HIGH);

  prevMainTop    = currTop;
  prevMainBottom = currBottom;

  // Nếu khởi động mà đã ở vị trí dừng → STOP
  if (currTop || currBottom) {
    mainDirection = 0x00;
  }

  sendFrame();
}

void loop() {
  bool currMainTop    = (digitalRead(REED_MAIN_TOP) == HIGH);     // LOW = kích hoạt
  bool currMainBottom = (digitalRead(REED_MAIN_BOTTOM) == HIGH);

  if (currMainTop && currMainBottom) {
    mainDirection = 0xFF;                    // ERROR
    lastMovingDirection = 0x00;              // Reset hướng khi lỗi
  }
  else if (currMainTop) {
    if (!prevMainTop) {                      // Vừa chạm TOP
      mainDirection = 0x01;                  // UP
      lastMovingDirection = 0x01;
    } 
  }
  else if (currMainBottom) {
    if (!prevMainBottom) {                   // Vừa chạm BOTTOM
      mainDirection = 0x02;                  // DOWN
      lastMovingDirection = 0x02;
    } 
  }
  else {
    // Không chạm reed nào → giữ nguyên hướng di chuyển trước đó
    mainDirection = lastMovingDirection;     // <-- Điểm thay đổi chính
  }

  // Cập nhật prev cho lần sau
  prevMainTop    = currMainTop;
  prevMainBottom = currMainBottom;

  sendFrame();
  delay(50);
}

void sendFrame() {


  Serial.write(STX);

  Serial.write(mainDirection);

  // Trạng thái reed chính thực tế
  bool topMain    = (digitalRead(REED_MAIN_TOP)    == HIGH);
  bool bottomMain = (digitalRead(REED_MAIN_BOTTOM) == HIGH);
  Serial.write(topMain    ? 0xFF : 0x00);
  Serial.write(bottomMain ? 0xFF : 0x00);

  // Container reeds
  Serial.write(digitalRead(REED_CON1_TOP)    == LOW ? 0xFF : 0x00);
  Serial.write(digitalRead(REED_CON1_BOTTOM) == LOW ? 0xFF : 0x00);
  Serial.write(digitalRead(REED_CON2_TOP)    == LOW ? 0xFF : 0x00);
  Serial.write(digitalRead(REED_CON2_BOTTOM) == LOW ? 0xFF : 0x00);
  Serial.write(digitalRead(REED_CON3_TOP)    == LOW ? 0xFF : 0x00);
  Serial.write(digitalRead(REED_CON3_BOTTOM) == LOW ? 0xFF : 0x00);
  Serial.write(digitalRead(REED_CON4_TOP)    == LOW ? 0xFF : 0x00);
  Serial.write(digitalRead(REED_CON4_BOTTOM) == LOW ? 0xFF : 0x00);

  Serial.write(ETX);
}