# Install script for directory: D:/dev/EtherCAT.NET/native/SOEM

# Set the install prefix
if(NOT DEFINED CMAKE_INSTALL_PREFIX)
  set(CMAKE_INSTALL_PREFIX "D:/dev/EtherCAT.NET/native/out/install/x64-Debug")
endif()
string(REGEX REPLACE "/$" "" CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}")

# Set the install configuration name.
if(NOT DEFINED CMAKE_INSTALL_CONFIG_NAME)
  if(BUILD_TYPE)
    string(REGEX REPLACE "^[^A-Za-z0-9_]+" ""
           CMAKE_INSTALL_CONFIG_NAME "${BUILD_TYPE}")
  else()
    set(CMAKE_INSTALL_CONFIG_NAME "Debug")
  endif()
  message(STATUS "Install configuration: \"${CMAKE_INSTALL_CONFIG_NAME}\"")
endif()

# Set the component getting installed.
if(NOT CMAKE_INSTALL_COMPONENT)
  if(COMPONENT)
    message(STATUS "Install component: \"${COMPONENT}\"")
    set(CMAKE_INSTALL_COMPONENT "${COMPONENT}")
  else()
    set(CMAKE_INSTALL_COMPONENT)
  endif()
endif()

# Is this installation the result of a crosscompile?
if(NOT DEFINED CMAKE_CROSSCOMPILING)
  set(CMAKE_CROSSCOMPILING "FALSE")
endif()

if("x${CMAKE_INSTALL_COMPONENT}x" STREQUAL "xUnspecifiedx" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "D:/dev/EtherCAT.NET/native/out/build/x64-Debug/SOEM/soem.lib")
endif()

if("x${CMAKE_INSTALL_COMPONENT}x" STREQUAL "xUnspecifiedx" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/soem" TYPE FILE FILES
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercat.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercatbase.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercatcoe.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercatconfig.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercatconfiglist.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercatdc.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercateoe.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercatfoe.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercatmain.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercatprint.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercatsoe.h"
    "D:/dev/EtherCAT.NET/native/SOEM/soem/ethercattype.h"
    "D:/dev/EtherCAT.NET/native/SOEM/osal/osal.h"
    "D:/dev/EtherCAT.NET/native/SOEM/osal/win32/inttypes.h"
    "D:/dev/EtherCAT.NET/native/SOEM/osal/win32/osal_defs.h"
    "D:/dev/EtherCAT.NET/native/SOEM/osal/win32/osal_win32.h"
    "D:/dev/EtherCAT.NET/native/SOEM/osal/win32/stdint.h"
    "D:/dev/EtherCAT.NET/native/SOEM/oshw/win32/nicdrv.h"
    "D:/dev/EtherCAT.NET/native/SOEM/oshw/win32/oshw.h"
    )
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  # Include the install script for each subdirectory.
  include("D:/dev/EtherCAT.NET/native/out/build/x64-Debug/SOEM/test/linux/slaveinfo/cmake_install.cmake")
  include("D:/dev/EtherCAT.NET/native/out/build/x64-Debug/SOEM/test/linux/eepromtool/cmake_install.cmake")
  include("D:/dev/EtherCAT.NET/native/out/build/x64-Debug/SOEM/test/linux/simple_test/cmake_install.cmake")

endif()

