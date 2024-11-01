# Measures that Matter (MtM)

## 🛠️ Overview

For the hackathon at Chicago Innovate - This project provides a solution for quickly collecting precise as-built measurement data in dynamic conditions, leveraging cutting-edge technology for real-time model updates. The process integrates Bluetooth-enabled measurement tools with augmented reality applications to streamline data capture and synchronization, reducing errors and improving project efficiency.

👩‍💻

## ⚙️ Hardware

- **Reekon T1 Tape Measure**  
  [Reekon T1](https://www.reekon.tools/)

  this uses the tape as a HID type  see instruction [here](https://support.reekon.tools/hc/en-us/articles/20026126134292-Keyboard-Mode-Bluetooth-Digital-Tape-Measure)
  
- **Bosch Laser**  
  [Bosch GLM 50 C](https://www.boschtools.com/us/en/products/glm-50-c-0601072C10)

## 💻 Software Stack

- **Fologram**  
  Augmented reality platform to visualize measurements in real-time on mobile and desktop.  
  [Fologram](https://fologram.com/)
  
- **Rhino 8 + Grasshopper**  
  Used for 3D modeling and computational design.
  
- **Python**  
  Bluetooth low energy communication using [Bleak](https://github.com/hbldh/bleak).

## 🔄 How It Works

Our solution integrates the **Reekon T1 Bluetooth Tape Measure** and future integration with the **Bosch Laser** to capture accurate measurements. These devices are tethered to a smartphone, which provides orientation and position data, ensuring precise alignment in the virtual model. The **Fologram** application visualizes the measurement data in real-time on both the phone and desktop, updating the digital model dynamically as measurements are taken.

### Data Flow

1. **Measurement Capture**: Measurements are taken using the Reekon T1 tape measure and Bosch laser, both of which communicate with the smartphone via Bluetooth.
2. **Phone Sensors**: The smartphone collects orientation and position data, tethering the measurements to the physical environment.
3. **Fologram Visualization**: Data from the measurement tools and the phone are displayed in Fologram, enabling real-time updates to the Rhino model.

## 🌟 Why It Matters

Traditional methods of as-built measurement collection are time-consuming, prone to human error, and often disconnected from the digital modeling process. Our system automates this process, allowing for quick, accurate, and dynamic measurement integration into the model, significantly reducing project timelines and improving data accuracy.

## 🚀 Getting Started

### Prerequisites

- **Reekon T1 Bluetooth Tape Measure**
- **Bosch GLM 50 C (optional for laser integration)**
- **Smartphone with Bluetooth capabilities**
- **Fologram App** (Available for both desktop and mobile)
- **Rhino 8 + Grasshopper** for computational design
- **Python** for Bluetooth communication via the `bleak` library

### Installation

1. Clone this repository:
   ```bash
   git clone https://github.com/3dm0nd6/measures-that-matter


### Install dependencies:
  ```bash
  Copy code
  pip install bleak
```
### Setup Fologram
- Download and install Fologram on your mobile device and desktop.
- Follow Fologram's setup guide to connect with Rhino and Grasshopper.

Pair Reekon T1 with Your Phone
- Ensure Bluetooth is enabled on your phone.
- Follow Reekon T1's instructions to pair it with your smartphone.
- Test the connection by taking a measurement and confirming it is sent to the phone.

### Running the Application
- Start Fologram on both your phone and desktop.
- Connect the Reekon T1 and, optionally, the Bosch laser to your computer via Bluetooth.
- Open the Rhino model and activate the Grasshopper script for real-time updates.
- Begin taking measurements, and watch the model dynamically update with precise data.

## 🔮 Future Work
- Bosch Laser Integration: Future updates will integrate additional laser measurement tools for even greater precision.
- Cloud Sync: Expand functionality to allow measurement data to be shared in real-time across teams.
- Custom Interface: Build an intuitive user interface for easy navigation and use of the system.

## 👥 Contributors

- Jo Kamm - Senior Design Technologist, DI
- Bryan V. - Programmer
- Hrushikesh Chavan - Architectural Designer, M.Arch, UIUC ‘24
- Kevin Mark - Full Stack Developer
- Alex Cardenas - Designer, Adrian Smith + Gordon Gill
- Edmond Abraham -Programmer
- Nathan Barnes - Computation Lead, Zahner

Thank you to [Chicago Innovate](https://www.chicagoinnovate.tech/) for a great time hacking!
