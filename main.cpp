#include "mbed.h"
#include "EthernetInterface.h"
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include "MCP4725.h"
#include "LittleFileSystem.h"
#include "SDBlockDevice.h"
#include <stdio.h>
#include <errno.h>
#include <string>
#include "platform/mbed_retarget.h"
#include "rtos.h"

#define ECHO_SERVER_PORT   80
//PC must setting 192.168.11.x
#define Server_IP "192.168.11.1"
#define Server_Gate "255.255.255.0"

#define TCP_MagicNum 2
#define TCP_PackageSize 512

#define  FileSaveRate 50
 // Network interface
EthernetInterface net;
nsapi_error_t result;

SDBlockDevice sd(MBED_CONF_SD_SPI_MOSI,
                 MBED_CONF_SD_SPI_MISO,
                 MBED_CONF_SD_SPI_CLK,
                 MBED_CONF_SD_SPI_CS,
                 150*1000000);
LittleFileSystem fs("sd", &sd);
  //Create an MCP4725 object at the default address (ADDRESS_0)
MCP4725 dac0(I2C_SDA,I2C_SCL);
MCP4725 dac1(I2C_SDA,I2C_SCL,MCP4725::ADDRESS_1);

InterruptIn Button(PD_6);//PD6

Thread thread;
volatile bool running = false;

//SD Card 
void return_error(int ret_val)
{
    if (ret_val) {
        printf("Failure. %d\n", ret_val);
        while (true) {
            __WFI();
        }
    } else {
        //printf("done.\n");
    }
}

void errno_error(void *ret_val)
{
    if (ret_val == NULL) {
        printf(" Failure. %d \n", 0);
        while (true) {
            __WFI();
        }
    } else {
        //printf(" done.\n");
    }
}
//
void OutputPulse()
{
    dac0.write(1);
    dac0.write(0);
          
}

void ClearByte(char* data,int len)
{
    for(int i =0;i<len;i++)
        data[i]='\0';
}

void ReceiveData(TCPSocket* client_sock, SocketAddress client_addr,bool Savefile)
{
    
    char *buffer = new char[TCP_PackageSize + TCP_MagicNum];
    //File Buffer
    char *Filebuffer = new char[(TCP_PackageSize)*FileSaveRate];
    ClearByte(buffer,TCP_PackageSize + TCP_MagicNum);
    //Read Total Counter
    client_sock->recv(buffer, TCP_PackageSize + TCP_MagicNum);
    client_sock->send(buffer, strlen(buffer));
    int total = atoi(buffer);
    printf("Received Total: %d\n", total);  //this was missing in original example. 
    //Try to OpenFile
    FILE* fd;
    if(Savefile){
        fd = fopen("/sd/numbers.txt", "w+");
        errno_error(fd);
    }
    int Fcounter = 0;
    //Reset Counter
    int counter = 0;
    //Receive Data
    while (counter < total)
    {
        nsapi_error_t recvResult = client_sock->recv(buffer, TCP_PackageSize + TCP_MagicNum);
        if (recvResult < 0)
            break;
        client_sock->send(buffer, strlen(buffer));
        counter += recvResult - TCP_MagicNum;
        memcpy(&Filebuffer[(Fcounter % FileSaveRate) * (TCP_PackageSize)], buffer, TCP_PackageSize);
        if (counter % (FileSaveRate * TCP_PackageSize) == 0)
        {
            Filebuffer[(FileSaveRate * TCP_PackageSize)] = '\0';
            if(Savefile)
                fprintf(fd, "%s", Filebuffer);
        }
        Fcounter++;
    }
    if (counter % (FileSaveRate * TCP_PackageSize) != 0)
    {
        Filebuffer[counter % (FileSaveRate * TCP_PackageSize)] = '\0';
        if(Savefile)
            fprintf(fd, "%s", Filebuffer);
        //printf("%s\n", Filebuffer); 
    }
    ClearByte(buffer,TCP_PackageSize + TCP_MagicNum);
    sprintf(buffer, "%d,%d", counter, client_addr.get_port());
    client_sock->send(buffer, strlen(buffer));
    printf("Received Msg: %d\n", counter);  //this was missing in original example. 
    if(Savefile)
        fclose(fd);
    //Free memory
    free(buffer) ;
    free(Filebuffer) ;
}
void OutputData(TCPSocket* client_sock, SocketAddress client_addr,bool IsSendData)
{
    //Read File Test
    FILE*  fd = fopen("/sd/numbers.txt", "r");
    errno_error(fd);
    //需要多1byte放結束字元
    //'X' 'X' 'X' 'X' '\0'
    char buff[5] = {0};
    int counter = 0;
    while (!feof(fd)) {
       int size = fread(&buff[0], 1, 4, fd);
       if(size!=0){
            //hex to int
            unsigned int x = std::stoul(buff, nullptr, 16);
            if(IsSendData)
                client_sock->send(buff, strlen(buff));
            counter++;
            if(counter%2==0)
            {
                dac0.write(x/65535.0);

            }else {
                dac1.write(x/65535.0);
            }
            //printf("%d\n",x);
       }
       //fwrite(&buff[0], 1, size, stdout);
    }
    fclose(fd);
}
void OutputDACThread()
{ 
    while (true) {
        if(running){
            printf("Prepare Output DAC...\n");
            //Read File Test
            FILE*  fd = fopen("/sd/numbers.txt", "r");
            errno_error(fd);
            //需要多1byte放結束字元
            //'X' 'X' 'X' 'X' '\0'
            char buff[5] = {0};
            int counter = 0;
            while (!feof(fd)) {
            if(!running)
                break;
            int size = fread(&buff[0], 1, 4, fd);
            if(size!=0){
                    //hex to int
                    unsigned int x = std::stoul(buff, nullptr, 16);
                    counter++;
                    if(counter%2==0)
                    {
                        dac0.write(x/65535.0);

                    }else {
                        dac1.write(x/65535.0);
                    }
                    //printf("%d\n",x);
            }
            //fwrite(&buff[0], 1, size, stdout);
            }
            fclose(fd);
            printf("Finish Output DAC...\n");
            running = false;
        }else {
           ThisThread::sleep_for(100);
        }
    }
}
void InitFileSystem()
{
    int errorcode = 0;
    errorcode = fs.mount(&sd);
    if (errorcode) {
        // Reformat if we can't mount the filesystem
        // this should only happen on the first boot
        printf("No filesystem found, formatting... ");
        errorcode = fs.reformat(&sd);
        return_error(errorcode);
    }
   
}

// main() runs in its own thread in the OS
int main()
{
    
    thread.start(OutputDACThread);
    InitFileSystem();
    //Interrup 
    Button.rise(&OutputPulse);
    //Try to open the MCP4725
    if (dac0.open()) {
         printf("Device[0] detected!\n");
          //Wake up the DAC
          //NOTE: This might wake up other I2C devices as well!
          dac0.wakeup();
          //while (1) {
             //Generate a sine wave on the DAC
              //for (float i = 0.0; i < 360.0; i += 0.1)
               //   dac.write( 0.5 * (sinf(i * 3.14159265 / 180.0) + 1));
          //}
      } else {
          error("Device[0] not detected!\n");
      }
    if (dac1.open()) {
         printf("Device[1] detected!\n");
          //Wake up the DAC
          //NOTE: This might wake up other I2C devices as well!
          dac1.wakeup();
      } else {
          error("Device[1] not detected!\n");
      }
   // Bring up the ethernet interface
    printf("Ethernet socket connect PC example\n");
   
    net.set_network(Server_IP, Server_Gate, "");
    net.connect();
    
      // Show the network address
    SocketAddress a;
    net.get_ip_address(&a);
    printf("IP address: %s\n", a.get_ip_address() ? a.get_ip_address() : "None");

    TCPSocket server;
    TCPSocket *client_sock; 
    SocketAddress client_addr;

    char *BufferMode = new char[10];

    result = server.open(&net);
     if (result < 0) {
        printf("Could not server.open(&net).\n\r");
        return -1;
    }
    result =  server.bind(ECHO_SERVER_PORT);
    if (result < 0) {
        printf("Could not server.bind(ECHO_SERVER_PORT).\n\r");
        return -1;
    }
    result = server.listen(1);
    if (result < 0) {
        printf("Could not server.listen().\n\r");
        return -1;
    }
    int counter = 0;
    while (true) {
        printf("\nWait for new connection...\n");
        client_sock = server.accept(&result);  //return pointer of a client socket
        //設置阻塞
        client_sock->set_blocking(false);
        //Set Timeout
        client_sock->set_timeout(1500);
        if(result==0)
        {
            client_sock->getpeername(&client_addr);  //this will fill address of client to the SocketAddress object
            printf("Accepted %s:%d\n", client_addr.get_ip_address(), client_addr.get_port());
            //Clear Byte
            ClearByte(BufferMode,10);
            //Read Mode Counter
            client_sock->recv(BufferMode, 10);
            int Mode= atoi(BufferMode);
            client_sock->send(BufferMode, strlen(BufferMode));
            printf("Received Mode: %d\n", Mode);  //this was missing in original example. 
            switch (Mode) {
                case 0:
                    //TransferData
                    printf("TransferData(true);\n");
                    ReceiveData(client_sock,client_addr,true);
                break;
                case 1:
                    //ReadData
                    printf("ReadData();\n");
                    OutputData(client_sock,client_addr,true);
                break;
                case 2:
                    //TransferData NoSave
                     printf("TransferData(false);\n");
                    ReceiveData(client_sock,client_addr,false);
                break;
                 case 3:
                    //Start
                    running=true;
                    printf("Start();\n");
                break;
                case 4:
                    //Stop
                    running=false;
                    printf("Stop();\n");
                break;
            }
            client_sock->close();
        }else {
            printf("Error:%d",result);
            break;
        }
    }
    // Bring down the ethernet interface
    net.disconnect();
    
}

