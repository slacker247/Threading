# builds the TestThreading project.

mkdir ./build
mkdir ./out

echo "" > build.log

cd ../Threading/
./build.sh
cd ../TestThreading/

echo "========== Start build of ThreadTest.cpp ========" >> build.log
g++ -c -fPIC -I./ ThreadTest.cpp -o build/ThreadTest.o >> build.log 2>&1
echo "========== Finished build of ThreadTest.cpp ========" >> build.log

echo "========== Start link of TestThreading ========" >> build.log
g++ -Wl,-rpath,/usr/local/lib/JAMSolutions/Apps/ -pthread TestThreading.cpp -o out/TestThreading -I./ /usr/local/lib/JAMSolutions/Apps/Threading.so $(ls build/*.o) >> build.log 2>&1
echo "========== Finished link of TestThreading ========" >> build.log

cat build.log | grep error

