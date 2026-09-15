import { useEffect } from 'react'
import { Button, Tooltip, Typography } from 'antd'
import { ArrowLeftOutlined, ArrowRightOutlined, ReloadOutlined } from '@ant-design/icons'
import { useSearchParams } from 'react-router-dom'
import { variants, type VariantKey } from './standardCatalogPrototypeData'
import { PrototypeStoreProvider, usePrototypeStore } from './standardCatalogPrototypeStore'
import { StandardCatalogVariantA } from './StandardCatalogVariantA'
import { StandardCatalogVariantAPlus } from './StandardCatalogVariantAPlus'
import { StandardCatalogVariantB } from './StandardCatalogVariantB'
import { StandardCatalogVariantC } from './StandardCatalogVariantC'
import './StandardCatalogPrototype.css'

/**
 * 标准项目目录维护页面的一次性 UI 原型。
 *
 * 变体共用同一份内存数据，通过 `?variant=A|A2|B|C` 切换；A2 为 A 的局部优化比较，
 * 差异只在结构与交互组织。本页面不是生产实现，生产代码不沿用其结构与样式。
 */
function PrototypeBody({ variant }: { variant: VariantKey }) {
  const store = usePrototypeStore()
  const active = variants.find((entry) => entry.key === variant) ?? variants[0]

  return (
    <div className={`sc-proto-page${variant === 'A2' ? ' sc-proto-page--aplus' : ''}`}>
      <div className='sc-proto-page__header'>
        <div>
          <Typography.Title level={4} style={{ margin: 0 }}>
            标准项目目录维护 · 页面形态原型
          </Typography.Title>
          <Typography.Text type='secondary'>
            当前变体 {active.key} · {active.name}
          </Typography.Text>
        </div>
        <Tooltip title='恢复初始内存数据'>
          <Button icon={<ReloadOutlined />} onClick={store.reset}>
            重置数据
          </Button>
        </Tooltip>
      </div>
      <Typography.Paragraph type='secondary' className='sc-proto-page__desc'>
        {active.structure}
      </Typography.Paragraph>
      {variant === 'A' && <StandardCatalogVariantA />}
      {variant === 'A2' && <StandardCatalogVariantAPlus />}
      {variant === 'B' && <StandardCatalogVariantB />}
      {variant === 'C' && <StandardCatalogVariantC />}
    </div>
  )
}

function VariantSwitcher({ current }: { current: VariantKey }) {
  const [, setSearchParams] = useSearchParams()
  const index = variants.findIndex((entry) => entry.key === current)
  const move = (offset: number) => {
    const next = variants[(index + offset + variants.length) % variants.length]
    setSearchParams({ variant: next.key }, { replace: true })
  }

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null
      if (event.defaultPrevented || target?.closest('input, textarea, select, button, [contenteditable="true"], [role="tree"], [role="menu"], [role="dialog"], [role="combobox"], [role="radiogroup"]')) return
      if (event.key === 'ArrowLeft') move(-1)
      if (event.key === 'ArrowRight') move(1)
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  })

  if (!import.meta.env.DEV) return null
  const active = variants[index]
  return (
    <div className='prototype-switcher' role='navigation' aria-label='原型方案切换'>
      <Tooltip title='上一个方案'>
        <Button type='text' aria-label='上一个方案' icon={<ArrowLeftOutlined />} onClick={() => move(-1)} />
      </Tooltip>
      <span>
        {active.key} · {active.name}
      </span>
      <Tooltip title='下一个方案'>
        <Button type='text' aria-label='下一个方案' icon={<ArrowRightOutlined />} onClick={() => move(1)} />
      </Tooltip>
    </div>
  )
}

export function StandardCatalogPrototype() {
  const [searchParams] = useSearchParams()
  const requested = searchParams.get('variant')?.toUpperCase()
  const variant: VariantKey = requested === 'A2' || requested === 'B' || requested === 'C' ? requested : 'A'

  return (
    <PrototypeStoreProvider>
      <PrototypeBody variant={variant} />
      <VariantSwitcher current={variant} />
    </PrototypeStoreProvider>
  )
}
