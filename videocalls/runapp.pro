QT += core gui widgets multimedia multimediawidgets network

CONFIG += c++17

TARGET = RunApp
TEMPLATE = app

SOURCES += \
    main.cpp \
    ui/mainwindow.cpp \
    ui/loginwidget.cpp \
    ui/feedwidget.cpp \
    ui/reelplayer.cpp \
    lyfron/crypto.c

HEADERS += \
    ui/mainwindow.h \
    ui/loginwidget.h \
    ui/feedwidget.h \
    ui/reelplayer.h \
    lyfron/lyfron.h \
    lyfron/crypto.h

LIBS += -lssl -lcrypto -largon2

# Platform specific
win32 {
    LIBS += -L$$PWD/libs/windows
}
unix {
    LIBS += -L/usr/local/lib
    INCLUDEPATH += /usr/local/include
}

RESOURCES += resources.qrc