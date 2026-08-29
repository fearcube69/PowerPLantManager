@echo off
:: Set Processor Maximum State to 100% on AC power
powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100

:: Apply changes immediately
powercfg /setactive SCHEME_CURRENT