// VCExeSample.c
// Generated by decompiling D:\dev\uxmal\reko\master\subjects\PE-x86\VCExeSample\VCExeSample.exe
// using Decompiler version 0.6.0.0.

#include "VCExeSample.h"

int32 main(int32 argc, char * * argv)
{
	test1(*argv, argc, "test123", 1.0F);
	return 0x00;
}

void test1(char * arg1, int32 arg2, char * arg3, real32 arg4)
{
	printf("%s %d %s %f");
	return;
}

void test2(word32 dwArg04)
{
	test1("1", 0x02, "3", (real32) globals->r4020E8);
	if (dwArg04 == 0x00)
		test1("5", 0x06, "7", (real32) globals->r4020E4);
	return;
}

void indirect_call_test3(cdecl_class * c)
{
	cdecl_class_vtbl * edx_15 = c->vtbl;
	 * eax_16 = edx_15->method04;
	word32 esp_17;
	word32 ebp_18;
	word32 eax_19;
	word32 ecx_20;
	word32 edx_21;
	byte SCZO_22;
	eax_16();
	return;
}

