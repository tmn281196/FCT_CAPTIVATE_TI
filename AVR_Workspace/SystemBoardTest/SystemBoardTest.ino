#define REED_TOP    22
#define REED_BOTTOM 23

bool prevTop    = false;   // trạng thái reed top ở lần đọc trước
bool prevBottom = false;   // trạng thái reed bottom ở lần đọc trước

void setup() {
  Serial.begin(9600);
  
  pinMode(REED_TOP,    INPUT_PULLUP);
  pinMode(REED_BOTTOM, INPUT_PULLUP);
  
  // Đọc giá trị ban đầu để tránh báo thay đổi giả lúc khởi động
  prevTop    = (digitalRead(REED_TOP)    == HIGH);
  prevBottom = (digitalRead(REED_BOTTOM) == HIGH);
  
  Serial.println("Bat dau - trang thai: DANG NGUNG (STOP)");
}

void loop() {
  bool currTop    = (digitalRead(REED_TOP)    == HIGH);
  bool currBottom = (digitalRead(REED_BOTTOM) == HIGH);

  byte currentState = 0;  // mặc định STOP

  if (currTop && currBottom) {
    currentState = 3;  // ERROR - cả hai reed cùng ON
  }
  else if (currTop) {
    if (prevTop == false) {
      // Vừa chạm reed top → bắt đầu đi lên hoặc vừa tới đỉnh
      currentState = 1;  // UP
    } else {
      // Đã ở reed top từ trước → đã dừng ở trên
      currentState = 0;  // STOP
    }
  }
  else if (currBottom) {
    if (prevBottom == false) {
      // Vừa chạm reed bottom → bắt đầu đi xuống hoặc vừa tới đáy
      currentState = 2;  // DOWN
    } else {
      // Đã ở reed bottom từ trước → đã dừng ở dưới
      currentState = 0;  // STOP
    }
  }
  else {
    // Không chạm reed nào → đang ở giữa → đang di chuyển (hoặc dừng giữa nếu không có động cơ)
    // Nếu hệ thống của bạn không dừng giữa, thì coi là đang di chuyển theo hướng cuối
    // Nhưng để chính xác, tốt nhất nên đọc thêm trạng thái động cơ/relay
    currentState = 0;  // STOP (an toàn nhất nếu không có thông tin động cơ)
  }

  // Cập nhật trạng thái trước cho lần sau
  prevTop    = currTop;
  prevBottom = currBottom;

  // In trạng thái
  Serial.print("Trang thai: ");
  if      (currentState == 0) Serial.print("DANG NGUNG (STOP)");
  else if (currentState == 1) Serial.print("DANG DI LEN (UP)");
  else if (currentState == 2) Serial.print("DANG DI XUONG (DOWN)");
  else                        Serial.print("LOI - CA HAI REED CUNG KICH HOAT");

  Serial.print("   |   Reed Top: ");
  Serial.print(currTop ? "ON" : "off");
  Serial.print("   |   Reed Bottom: ");
  Serial.print(currBottom ? "ON" : "off");
  Serial.println();

  delay(100);  // Giảm delay để phát hiện thay đổi nhanh hơn
}