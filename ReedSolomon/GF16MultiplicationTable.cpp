#include "pch.h"
#include "GF16MultiplicationTable.h"
#include <iostream>
#include "GF16.h"

namespace ReedSolomon {

	bool GF16MultiplicationTable::initialized = GF16MultiplicationTable::staticInitialize();

	bool GF16MultiplicationTable::staticInitialize() {
		int tableElementCount = 256;
		int tableBitCount = 8;

		polynomial = _mm_set1_epi16(PRIMITIVE_POLYNOMIAL);

		int *current = lookupIndices;
		int increment = 2;
		int count = tableElementCount / 2;
		for (int i = 0; i < tableBitCount; i++) {
			for (int j = 0; j < count; j++, current += increment) *current = i;
			current = lookupIndices + increment - 1;
			count >>= 1;
			increment <<= 1;
		}

		return true;
	}

	GF16MultiplicationTable::GF16MultiplicationTable() {
		low = (__m128i*)_aligned_malloc(256 * 16, 16);
		high = (__m128i*)_aligned_malloc(256 * 16, 64);
	}

	void GF16MultiplicationTable::Set(const __m128i& x) {

		__declspec(align(16)) __m128i addTable[8];
		__m128i add = x;

		addTable[0] = add;
		for (int i = 1; i < 8; i++) {
			// add *= 2
			add = _mm_xor_si128(_mm_and_si128(polynomial, _mm_srai_epi16(add, 16)), _mm_slli_epi16(add, 1));
			addTable[i] = _mm_xor_si128(addTable[i - 1], add);
		}

		low[0] = _mm_setzero_si128();
		for (int i = 1; i < 256; i++) {
			low[i] = _mm_xor_si128(low[i - 1], addTable[lookupIndices[i - 1]]);
		}

		addTable[0] = add;
		for (int i = 1; i < 8; i++) {
			// add *= 2
			add = _mm_xor_si128(_mm_and_si128(polynomial, _mm_srai_epi16(add, 16)), _mm_slli_epi16(add, 1));
			addTable[i] = _mm_xor_si128(addTable[i - 1], add);
		}

		high[0] = _mm_setzero_si128();
		for (int i = 1; i < 256; i++) {
			high[i] = _mm_xor_si128(high[i - 1], addTable[lookupIndices[i - 1]]);
		}
	}

	void GF16MultiplicationTable::MultiplyAndXor(uint16_t* coefficients, __m128i* dest, int count) {
		char* bytes = (char*)coefficients;
		for (int i = 0; i < count; i++, dest++) {
			//__m128i lowValue = *(low + *bytes);
			__m128i lowValue = _mm_load_si128(low + *bytes);
			bytes++;
			//__m128i highValue = *(high + *bytes);
			__m128i highValue = _mm_load_si128(high + *bytes);
			bytes++;
			__m128i res = _mm_xor_si128(lowValue, highValue);
			__m128i tmp = _mm_load_si128(dest);
			tmp = _mm_xor_si128(res, tmp);
			_mm_store_si128(dest, tmp);
		}
	}

	GF16MultiplicationTable::~GF16MultiplicationTable() {
		_aligned_free(low);
		_aligned_free(high);
	}

	int GF16MultiplicationTable::lookupIndices[256];
	__declspec(align(16)) __m128i GF16MultiplicationTable::polynomial;
}