// redir.c
// Generated by decompiling redir
// using Reko decompiler version 0.7.2.0.

#include "redir.h"

int8 g_aFFFFFFEC[];
<anonymous> g_tFFFFFFFF;
byte g_b0002;
int32 g_dw01C4;
Eq_222 g_t0695;
<anonymous> g_t811F260;
Eq_36 g_t10000000 = 
	{
		&g_ptr10000860,
		0,
		0,
		&g_tFFFFFFFF,
	};
int32 g_dw10000014 = 0;
int32 g_dw10000018 = 4;
int32 g_dw1000001C = 204800;
int32 g_dw10000020 = 0;
Eq_222 g_t10000024 = 
	{
		
		{
		},
		4,
		0x00000000,
		300,
		0x000000C8,
		47,
	};
word32 g_a100007E0[] = 
	{
	};
<anonymous> * g_ptr10000860 = null;
struct Eq_36 * g_ptr10000878 = &g_t10000000;
ptr32 g_ptr1000087C = 0x00400000;
ptr32 g_ptr10000880 = 0x00410000;
<anonymous> * g_ptr10000894 = __pack_d;
<anonymous> * g_ptr10000898 = client_check_activ;
int8 ** g_ptr100008A0 = &g_ptr10000AAC;
<anonymous> * g_ptr100008A4 = &g_t811F260;
<anonymous> * g_ptr100008A8 = slist_destroy;
<anonymous> * g_ptr100008AC = get_a_line;
<anonymous> * g_ptr100008B0 = slist_add;
<anonymous> * g_ptr100008B4 = request_get_host;
<anonymous> * g_ptr100008BC = client_close;
int32 * g_ptr100008C0 = &g_dw10000014;
<anonymous> * g_ptr100008D0 = open_log;
<anonymous> * g_ptr100008DC = client_destroy;
<anonymous> * g_ptr100008E0 = slist_delete;
<anonymous> * g_ptr100008E4 = open_destination;
<anonymous> * g_ptr100008E8 = get_version;
<anonymous> * g_ptr100008EC = request_add_lines;
int32 * g_ptr100008F4 = &g_dw10000018;
<anonymous> * g_ptr10000908 = clist_new;
int32 * g_ptr1000090C = &g_dw10000020;
int32 * g_ptr10000914 = &g_dw01C4;
<anonymous> * g_ptr10000918 = clist_add;
<anonymous> * g_ptr10000928 = clist_close_all;
<anonymous> * g_ptr1000092C = clist_destroy_all;
<anonymous> * g_ptr10000938 = client_new;
<anonymous> * g_ptr10000940 = get_uri;
<anonymous> * g_ptr10000954 = request_save_line;
<anonymous> * g_ptr10000958 = client_copy_reply;
word32 g_dw10000964 = 0x00402244;
<anonymous> * g_ptr10000968 = client_read_request;
<anonymous> * g_ptr10000970 = slist_remove;
<anonymous> * g_ptr10000974 = server_close;
<anonymous> * g_ptr10000978 = __fpcmp_parts_d;
<anonymous> * g_ptr1000097C = request_parse_line;
<anonymous> * g_ptr10000984 = slist_close_all;
<anonymous> * g_ptr10000990 = clist_destroy;
<anonymous> * g_ptr10000998 = get_method;
<anonymous> * g_ptr100009A4 = client_parse_reply;
<anonymous> * g_ptr100009A8 = properties_load;
<anonymous> * g_ptr100009AC = __pack_f;
<anonymous> * g_ptr100009B0 = client_send_request;
<anonymous> * g_ptr100009B4 = &g_t811F260;
word32 (* g_ptr100009C8)[] = &g_a100007E0;
<anonymous> * g_ptr100009CC = slist_new;
word32 g_dw100009D0 = 0x004021A0;
<anonymous> * g_ptr100009DC = __unpack_f;
<anonymous> * g_ptr100009E0 = add_to_request;
<anonymous> * g_ptr100009EC = print_log;
<anonymous> * g_ptr100009FC = request_get_content_length;
<anonymous> * g_ptr10000A00 = server_destroy;
<anonymous> * g_ptr10000A04 = slist_destroy_all;
<anonymous> * g_ptr10000A08 = server_new;
<anonymous> * g_ptr10000A10 = __unpack_d;
<anonymous> * g_ptr10000A14 = clist_delete;
int32 * g_ptr10000A20 = &g_dw10000AA8;
int32 * g_ptr10000A28 = &g_dw1000001C;
<anonymous> * g_ptr10000A38 = request_make_url;
struct Eq_222 * g_ptr10000A40 = &g_t10000024;
<anonymous> * g_ptr10000A44 = clist_remove;
<anonymous> * g_ptr10000A50 = client_read_reply;
<anonymous> * g_ptr10000A58 = client_prepare_connect;
<anonymous> * g_ptr10000A5C = properties_parse_command_line;
<anonymous> * g_ptr10000A64 = __make_fp;
<anonymous> * g_ptr10000A68 = __make_dp;
<anonymous> * g_ptr10000A70 = log_rotate;
<anonymous> * g_ptr10000A80 = request_destroy;
<anonymous> * g_ptr10000A84 = server_open;
<anonymous> * g_ptr10000A94 = request_new;
<anonymous> * g_ptr10000A98 = client_check_reply_http;
<anonymous> * g_ptr10000A9C = properties_print_usage;
<anonymous> * g_ptr10000AA0 = is_a_method;
int32 g_dw10000AA8 = 0;
int8 * g_ptr10000AAC = null;

