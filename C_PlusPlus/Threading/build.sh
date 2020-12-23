# builds the Threading project.

mkdir -p /usr/local/lib/JAMSolutions/Apps/
mkdir ./build

echo "" > build.log

echo "========== Start build of ActiveObject.cpp ========" >> build.log
g++ -c -fPIC -I./ ActiveObject.cpp -o build/ActiveObject.o >> build.log 2>&1
echo "========== Finished build of ActiveObject.cpp ========" >> build.log

echo "========== Start build of Event.cpp ========" >> build.log
g++ -c -fPIC -I./ Event.cpp -o build/Event.o >> build.log 2>&1
echo "========== Finished build of Event.cpp ========" >> build.log

echo "========== Start build of Mutex.cpp ========" >> build.log
g++ -c -fPIC -I./ Mutex.cpp -o build/Mutex.o >> build.log 2>&1
echo "========== Finished build of Mutex.cpp ========" >> build.log

echo "========== Start build of Thread.cpp ========" >> build.log
g++ -c -fPIC -I./ Thread.cpp -o build/Thread.o >> build.log 2>&1
echo "========== Finished build of Thread.cpp ========" >> build.log

echo "========== Start link of Threading ========" >> build.log
g++ -shared -Wl,-soname,Threading.so -o /usr/local/lib/JAMSolutions/Apps/Threading.so $(ls build/*.o) >> build.log 2>&1
echo "========== Finished link of Threading ========" >> build.log

cat build.log | grep error

