import RPi.GPIO as GPIO
import time
import requests
import sys
import logging
import signal
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry
from calendar import monthrange
from datetime import datetime

#Pins
IR     = 9
BUZZER = 18
LCD_RS = 2
LCD_E  = 23
LCD_D4 = 24
LCD_D5 = 17
LCD_D6 = 27
LCD_D7 = 22

#Width
LCD_WIDTH = 16

#Send data but don't send commands
LCD_CHR = True
LCD_CMD = False

#Line addresses
LCD_LINE_1 = 0x80
LCD_LINE_2 = 0xC0

#500ns pulse width and delay
E_PULSE = 0.0005
E_DELAY = 0.0005

#Web url
URL = 'https://api.tzer0m.co.uk/Hours'

#Set logger
logger = logging.getLogger()
logging.basicConfig(level=logging.INFO, stream=sys.stdout)

#Reusable session for the whole process lifetime
session = requests.Session()
retry = Retry(connect=3, backoff_factor=0.5)
adapter = HTTPAdapter(max_retries=retry)
session.mount('http://', adapter)
session.mount('https://', adapter)


def gpio_init():
        """Initialise the IR receiver and LCD in 4-bit mode."""
        logging.info("Setting pins")
        GPIO.setmode(GPIO.BCM)
        GPIO.setwarnings(False)
        GPIO.setup(IR, GPIO.IN, pull_up_down=GPIO.PUD_DOWN)
        GPIO.setup(BUZZER, GPIO.OUT)
        for pin in [LCD_RS, LCD_E, LCD_D4, LCD_D5, LCD_D6, LCD_D7]:
                GPIO.setup(pin, GPIO.OUT)
                GPIO.output(pin, False)

        logging.info("Configuring LCD")
        lcd_byte(0x33, LCD_CMD)
        lcd_byte(0x32, LCD_CMD)
        lcd_byte(0x28, LCD_CMD)
        lcd_byte(0x0C, LCD_CMD)
        lcd_byte(0x06, LCD_CMD)
        lcd_byte(0x01, LCD_CMD)
        time.sleep(E_DELAY)


def lcd_byte(bits, mode):
        """Send a byte to the LCD (high nibble first, then low nibble)."""
        GPIO.output(LCD_RS, mode)

        GPIO.output(LCD_D4, bool(bits & 0x10))
        GPIO.output(LCD_D5, bool(bits & 0x20))
        GPIO.output(LCD_D6, bool(bits & 0x40))
        GPIO.output(LCD_D7, bool(bits & 0x80))
        _pulse_enable()

        GPIO.output(LCD_D4, bool(bits & 0x01))
        GPIO.output(LCD_D5, bool(bits & 0x02))
        GPIO.output(LCD_D6, bool(bits & 0x04))
        GPIO.output(LCD_D7, bool(bits & 0x08))
        _pulse_enable()


def _pulse_enable():
        """Toggle the Enable pin to latch data."""
        GPIO.output(LCD_E, False)
        time.sleep(E_PULSE)
        GPIO.output(LCD_E, True)
        time.sleep(E_PULSE)
        GPIO.output(LCD_E, False)
        time.sleep(E_DELAY)


def lcd_string(message, line):
        """Write a string to the given line (LCD_LINE_1 or LCD_LINE_2)."""
        logging.info('Setting %s to "%s"' % (line, message))
        message = message.ljust(LCD_WIDTH)
        lcd_byte(line, LCD_CMD)
        for char in message:
                lcd_byte(ord(char), LCD_CHR)


def lcd_clear():
        lcd_byte(0x01, LCD_CMD)
        time.sleep(E_DELAY)


def on_button_press(channel):
        """Called by GPIO interrupt when the IR sensor triggers."""
        logging.info("Button pressed, sending request")
        try:
                response = session.get(URL, timeout=10)

                logging.info("Calculating values")
                currentHours = float(response.text)
                targetHours = monthrange(int(datetime.today().strftime('%Y')), int(datetime.today().strftime('%m')))[1] * 4
                percentage = currentHours / targetHours

                logging.info("Calculating difference")
                difference = (currentHours - (int(datetime.today().strftime('%d')) * 4)) / 4
                if difference > 0:
                        lcd_string('%s Days Ahead' % "{:.2f}".format(difference), LCD_LINE_2)
                elif difference < 0:
                        lcd_string('%s Days Behind' % "{:.2f}".format(abs(difference)), LCD_LINE_2)
                else:
                        lcd_string("On Target", LCD_LINE_2)

                lcd_string('%s/%s - %s%%' % ("{:.2f}".format(currentHours), "{:.0f}".format(targetHours), "{:.0f}".format(percentage * 100)), LCD_LINE_1)

                if len(sys.argv) > 1 and sys.argv[1] == "True":
                        logging.info("Sounding buzzer")
                        GPIO.output(BUZZER, True)
                        time.sleep(0.05)
                        GPIO.output(BUZZER, False)
                        time.sleep(0.25)

        except requests.exceptions.ConnectionError:
                logging.error("Connection failed - check network/URL")
                lcd_string("Error", LCD_LINE_1)
                lcd_string("No Connection", LCD_LINE_2)
        except requests.exceptions.Timeout:
                logging.error("Request timed out")
                lcd_string("Error", LCD_LINE_1)
                lcd_string("Timeout", LCD_LINE_2)
        except requests.exceptions.RequestException as e:
                logging.error(f"Request error: {e}")
                lcd_string("Error", LCD_LINE_1)
                lcd_string("Request Error", LCD_LINE_2)
        except ValueError:
                logging.error("Invalid response - couldn't parse hours")
                lcd_string("Error", LCD_LINE_1)
                lcd_string("Bad Response", LCD_LINE_2)


if __name__ == "__main__":
        try:
                gpio_init()
                lcd_string("WELCOME", LCD_LINE_1)

                GPIO.add_event_detect(IR, GPIO.FALLING, callback=on_button_press, bouncetime=1000)

                signal.pause()

        except KeyboardInterrupt:
                logging.warning("Exiting")
        finally:
                lcd_clear()
                GPIO.cleanup()
