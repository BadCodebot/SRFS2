#include "stdafx.h"
#include "Vector.h"
#include "GF16.h"
#include <iostream>

namespace ReedSolomon {

	Vector::Vector(int length) {
		elements = new uint16_t[length];
		this->length = length;
		memset(elements, 0, length * sizeof(uint16_t));
	}

	Vector::Vector(const Vector& v) {
		elements = new uint16_t[v.length];
		length = v.length;
		memcpy(elements, v.elements, length * sizeof(uint16_t));
	}

	Vector::~Vector() {
		delete[] elements;
	}

	void Vector::operator=(const Vector& v) {
		if (length != v.length) {
			delete[] elements;
			elements = new uint16_t[v.length];
		}
		memcpy(elements, v.elements, length * sizeof(uint16_t));
	}

	Vector& Vector::operator+=(const Vector& v) {
		for (int i = 0; i < length; i++) elements[i] = GF16::Add(elements[i], v.elements[i]);
		return *this;
	}

	Vector& Vector::operator*=(uint16_t a) {
		for (int i = 0; i < length; i++) elements[i] = GF16::Multiply(a, elements[i]);
		return *this;
	}

	Vector Vector::operator*(uint16_t a) const {
		Vector v(*this);
		v *= a;
		return v;
	}

	uint16_t InnerProduct(const Vector& a, const Vector& b) {
		uint16_t rv = 0;
		for (int i = 0; i < a.GetLength(); ++i) GF16::Add(rv, GF16::Multiply(a[i], b[i]));
		return rv;
	}

	Vector operator*(uint16_t a, const Vector& v) { return v * a; }
}
