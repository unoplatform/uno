import sys
import time
from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from webdriver_manager.chrome import ChromeDriverManager

if len(sys.argv) != 2:
    printf("Incorrect number of arguments, expected 1 argument for startup url.", sys.argv)
    sys.exit(1)

service = Service(ChromeDriverManager().install())
options = webdriver.ChromeOptions()
options.add_argument('--verbose')
options.add_argument('--no-sandbox')
options.add_argument('--enable-logging=stderr')
options.add_argument('--disable-dev-shm-usage')
options.add_argument('--headless')
driver = webdriver.Chrome(service=service, options=options)

driver.get(sys.argv[1])


time.sleep(999999)
driver.quit()

